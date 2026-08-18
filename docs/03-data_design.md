
# EdgePulse -- Data Design Document

**Version:** 1.0
**Status:** Approved
**Last Updated:** May 2026
**Author:** Rakshith N S
**Depends On:** 01-requirements.md v1.0, 02-architecture.md v1.0

> **📌 As-built note (August 2026, v1.0.0 shipped).** This is the original
> pre-Sprint-1 design document, kept as approved for provenance. The design
> held; three things differ in what was actually built and shipped:
> (1) the **on-premise profile is what runs and is verified** — RabbitMQ,
> MongoDB and SQL Server (the Azure Service Bus / Cosmos DB / Azure SQL
> profile is the documented cloud mapping, selected by `DEPLOYMENT_MODE`);
> (2) the .NET projects live under `src/backend/`; (3) AI features are deferred
> post-v1.0. For the as-built system see `docs/guides/04-technical-guide.md`.
---

## Table of Contents

1. [Data Architecture Overview](#1-data-architecture-overview)
2. [Database Responsibilities](#2-database-responsibilities)
3. [Azure SQL -- Schema Design](#3-azure-sql--schema-design)
4. [Configurable Lookup Tables](#4-configurable-lookup-tables)
5. [Entity Relationship Diagram](#5-entity-relationship-diagram)
6. [Cosmos DB -- Telemetry Design](#6-cosmos-db--telemetry-design)
7. [PostgreSQL -- Keycloak Schema](#7-postgresql--keycloak-schema)
8. [On-Premise Database Design](#8-on-premise-database-design)
9. [Tenant Isolation Strategy](#9-tenant-isolation-strategy)
10. [Soft Delete Strategy](#10-soft-delete-strategy)
11. [Audit Log Design](#11-audit-log-design)
12. [Read Replica Strategy](#12-read-replica-strategy)
13. [Telemetry Retention Strategy](#13-telemetry-retention-strategy)
14. [Data Flow Diagrams](#14-data-flow-diagrams)
15. [Indexing Strategy](#15-indexing-strategy)
16. [Scalability Decisions](#16-scalability-decisions)

---

## 1. Data Architecture Overview

EdgePulse uses three databases, each chosen for a specific workload:

```
+==========================================================+
|                  DATA ARCHITECTURE                       |
|                                                          |
|  +------------------+  Purpose: Structured relational    |
|  |   Azure SQL      |  data -- devices, users, alerts,   |
|  |   (Primary DB)   |  audit logs, config                |
|  +------------------+  Engine: SQL Server 2022           |
|                        ORM: EF Core 9                    |
|                                                          |
|  +------------------+  Purpose: High-volume time-series  |
|  |   Cosmos DB      |  telemetry readings from devices   |
|  |   (Telemetry)    |  Partition: deviceId               |
|  +------------------+  TTL: 12 months auto-expire        |
|                                                          |
|  +------------------+  Purpose: Keycloak identity data   |
|  |   PostgreSQL     |  Users, sessions, realms, clients  |
|  |   (Identity)     |  Managed entirely by Keycloak      |
|  +------------------+  Never accessed by app directly    |
|                                                          |
+==========================================================+
```

### Design Decisions Summary

```
+---------------------------+--------------------------------+
| Decision                  | Choice                         |
+---------------------------+--------------------------------+
| Delete strategy           | Soft delete on all entities    |
|                           | IsDeleted flag + DeletedAt     |
+---------------------------+--------------------------------+
| Audit logging             | Separate AuditLog table        |
|                           | Immutable, append-only         |
+---------------------------+--------------------------------+
| Telemetry retention       | 12-month TTL + archive to      |
|                           | cold storage (future)          |
+---------------------------+--------------------------------+
| Read scalability          | Read replica for reports       |
|                           | Separate connection strings    |
+---------------------------+--------------------------------+
| Tenant isolation          | Global Query Filters in EF     |
|                           | TenantId on every table        |
+---------------------------+--------------------------------+
| Timestamps                | All stored in UTC              |
|                           | Displayed in mill local time   |
+---------------------------+--------------------------------+
```

---

## 2. Database Responsibilities

```
AZURE SQL owns:
  Tenants           -- customer organisations
  Mills             -- physical facilities
  Areas             -- departments / production lines
  Devices           -- physical equipment
  DeviceApiKeys     -- per-device telemetry auth keys
  AlertThresholds   -- configurable limits per device/metric
  Alerts            -- anomaly events with AI summaries
  AlertAssignments  -- which operator handles which alert
  Notifications     -- in-app notification records
  AuditLogs         -- immutable action history
  UserProfiles      -- EdgePulse-specific user data
                       (identity is in Keycloak/PostgreSQL)

COSMOS DB owns:
  TelemetryReadings -- time-series sensor data
                       high volume, schema-flexible
                       partitioned by deviceId
                       12-month TTL

POSTGRESQL owns:
  Everything Keycloak needs internally
  Never touched by EdgePulse application code
```

---

## 3. Azure SQL -- Schema Design

### 3.1 Tenants Table

```sql
CREATE TABLE Tenants (
    Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    Name            NVARCHAR(200)       NOT NULL,
    Slug            NVARCHAR(100)       NOT NULL,  -- url-safe name e.g. nordpulp
    ContactEmail    NVARCHAR(300)       NOT NULL,
    Status          NVARCHAR(20)        NOT NULL DEFAULT 'Active',
    -- Status values: Active, Suspended, Deleted
    CreatedAt       DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    IsDeleted       BIT                 NOT NULL DEFAULT 0,
    DeletedAt       DATETIME2           NULL,

    CONSTRAINT PK_Tenants PRIMARY KEY (Id),
    CONSTRAINT UQ_Tenants_Slug UNIQUE (Slug)
);
```

### 3.2 Mills Table

```sql
CREATE TABLE Mills (
    Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    TenantId        UNIQUEIDENTIFIER    NOT NULL,
    Name            NVARCHAR(200)       NOT NULL,
    Code            NVARCHAR(20)        NOT NULL,  -- e.g. LW (Lakewood)
    Location        NVARCHAR(300)       NOT NULL,  -- City, Country
    Timezone        NVARCHAR(100)       NOT NULL,  -- e.g. Europe/Helsinki
    HasInternet     BIT                 NOT NULL DEFAULT 1,
    DeploymentMode  NVARCHAR(20)        NOT NULL DEFAULT 'cloud',
    -- DeploymentMode: cloud, onpremise
    CreatedAt       DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    IsDeleted       BIT                 NOT NULL DEFAULT 0,
    DeletedAt       DATETIME2           NULL,

    CONSTRAINT PK_Mills PRIMARY KEY (Id),
    CONSTRAINT FK_Mills_Tenants FOREIGN KEY (TenantId)
        REFERENCES Tenants(Id),
    CONSTRAINT UQ_Mills_TenantCode UNIQUE (TenantId, Code)
);
```

### 3.3 Areas Table

```sql
CREATE TABLE Areas (
    Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    TenantId        UNIQUEIDENTIFIER    NOT NULL,
    MillId          UNIQUEIDENTIFIER    NOT NULL,
    Name            NVARCHAR(200)       NOT NULL,
    Code            NVARCHAR(20)        NOT NULL,  -- e.g. PM1
    Description     NVARCHAR(500)       NULL,
    CreatedAt       DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    IsDeleted       BIT                 NOT NULL DEFAULT 0,
    DeletedAt       DATETIME2           NULL,

    CONSTRAINT PK_Areas PRIMARY KEY (Id),
    CONSTRAINT FK_Areas_Mills FOREIGN KEY (MillId)
        REFERENCES Mills(Id),
    CONSTRAINT UQ_Areas_MillCode UNIQUE (MillId, Code)
);
```

### 3.4 Devices Table

```sql
CREATE TABLE Devices (
    Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    TenantId        UNIQUEIDENTIFIER    NOT NULL,
    MillId          UNIQUEIDENTIFIER    NOT NULL,
    AreaId          UNIQUEIDENTIFIER    NOT NULL,
    Name            NVARCHAR(200)       NOT NULL,
    Code            NVARCHAR(50)        NOT NULL,  -- e.g. PUMP-LW-001
    Type            NVARCHAR(50)        NOT NULL,
    -- Type: Pump, Motor, Valve, Sensor, Compressor, Fan, Other
    Manufacturer    NVARCHAR(200)       NULL,
    Model           NVARCHAR(200)       NULL,
    SerialNumber    NVARCHAR(100)       NULL,
    InstallDate     DATE                NULL,
    Status          NVARCHAR(20)        NOT NULL DEFAULT 'Offline',
    -- Status: Online, Offline, Maintenance, Decommissioned
    LastSeenAt      DATETIME2           NULL,
    CreatedAt       DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    IsDeleted       BIT                 NOT NULL DEFAULT 0,
    DeletedAt       DATETIME2           NULL,

    CONSTRAINT PK_Devices PRIMARY KEY (Id),
    CONSTRAINT FK_Devices_Areas FOREIGN KEY (AreaId)
        REFERENCES Areas(Id),
    CONSTRAINT UQ_Devices_TenantCode UNIQUE (TenantId, Code)
);
```

### 3.5 DeviceApiKeys Table

```sql
CREATE TABLE DeviceApiKeys (
    Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    TenantId        UNIQUEIDENTIFIER    NOT NULL,
    DeviceId        UNIQUEIDENTIFIER    NOT NULL,
    KeyHash         NVARCHAR(256)       NOT NULL,
    -- API key is hashed (SHA-256) -- never stored in plain text
    -- Plain text key shown ONCE at generation, then discarded
    KeyPrefix       NVARCHAR(10)        NOT NULL,
    -- First 8 chars of key for display e.g. "dev_a1b2"
    IsActive        BIT                 NOT NULL DEFAULT 1,
    ExpiresAt       DATETIME2           NULL,
    -- NULL = never expires
    LastUsedAt      DATETIME2           NULL,
    CreatedAt       DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    RevokedAt       DATETIME2           NULL,
    RevokedReason   NVARCHAR(200)       NULL,

    CONSTRAINT PK_DeviceApiKeys PRIMARY KEY (Id),
    CONSTRAINT FK_DeviceApiKeys_Devices FOREIGN KEY (DeviceId)
        REFERENCES Devices(Id),
    CONSTRAINT UQ_DeviceApiKeys_Hash UNIQUE (KeyHash)
);
```

### 3.6 AlertThresholds Table

```sql
CREATE TABLE AlertThresholds (
    Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    TenantId        UNIQUEIDENTIFIER    NOT NULL,
    DeviceId        UNIQUEIDENTIFIER    NOT NULL,
    Metric          NVARCHAR(100)       NOT NULL,
    -- e.g. temperature, pressure, vibration, flow_rate, power
    MinValue        DECIMAL(18,4)       NULL,
    -- NULL = no minimum threshold
    MaxValue        DECIMAL(18,4)       NULL,
    -- NULL = no maximum threshold
    Unit            NVARCHAR(20)        NOT NULL,
    Severity        NVARCHAR(20)        NOT NULL DEFAULT 'High',
    -- Severity: Critical, High, Medium, Low
    ConsecutiveCount INT                NOT NULL DEFAULT 3,
    -- How many consecutive breaches trigger alert
    IsActive        BIT                 NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_AlertThresholds PRIMARY KEY (Id),
    CONSTRAINT FK_AlertThresholds_Devices FOREIGN KEY (DeviceId)
        REFERENCES Devices(Id),
    CONSTRAINT UQ_AlertThresholds_DeviceMetric
        UNIQUE (DeviceId, Metric)
);
```

### 3.7 Alerts Table

```sql
CREATE TABLE Alerts (
    Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    TenantId        UNIQUEIDENTIFIER    NOT NULL,
    DeviceId        UNIQUEIDENTIFIER    NOT NULL,
    MillId          UNIQUEIDENTIFIER    NOT NULL,
    AreaId          UNIQUEIDENTIFIER    NOT NULL,
    Metric          NVARCHAR(100)       NOT NULL,
    TriggerValue    DECIMAL(18,4)       NOT NULL,
    ThresholdValue  DECIMAL(18,4)       NOT NULL,
    Unit            NVARCHAR(20)        NOT NULL,
    Severity        NVARCHAR(20)        NOT NULL,
    Status          NVARCHAR(20)        NOT NULL DEFAULT 'Open',
    -- Status: Open, Acknowledged, Assigned, Resolved, Closed
    AiSummary       NVARCHAR(MAX)       NULL,
    -- AI-generated human readable description
    ReadingsJson    NVARCHAR(MAX)       NULL,
    -- JSON array of the 3 consecutive readings that triggered alert
    -- e.g. [{"ts":"2026-05-13T14:30:15Z","value":85.1}, ...]
    TriggeredAt     DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    AcknowledgedAt  DATETIME2           NULL,
    AcknowledgedBy  NVARCHAR(200)       NULL,  -- userId
    ResolvedAt      DATETIME2           NULL,
    ResolvedBy      NVARCHAR(200)       NULL,  -- userId
    ClosedAt        DATETIME2           NULL,
    Notes           NVARCHAR(MAX)       NULL,
    CreatedAt       DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    IsDeleted       BIT                 NOT NULL DEFAULT 0,
    DeletedAt       DATETIME2           NULL,

    CONSTRAINT PK_Alerts PRIMARY KEY (Id),
    CONSTRAINT FK_Alerts_Devices FOREIGN KEY (DeviceId)
        REFERENCES Devices(Id)
);
```

### 3.8 AlertAssignments Table

```sql
CREATE TABLE AlertAssignments (
    Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    TenantId        UNIQUEIDENTIFIER    NOT NULL,
    AlertId         UNIQUEIDENTIFIER    NOT NULL,
    AssignedToUserId NVARCHAR(200)      NOT NULL,  -- Keycloak userId
    AssignedByUserId NVARCHAR(200)      NOT NULL,  -- Keycloak userId
    AssignedAt      DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    Notes           NVARCHAR(500)       NULL,

    CONSTRAINT PK_AlertAssignments PRIMARY KEY (Id),
    CONSTRAINT FK_AlertAssignments_Alerts FOREIGN KEY (AlertId)
        REFERENCES Alerts(Id)
);
```

### 3.9 UserProfiles Table

```sql
-- EdgePulse-specific user data.
-- Identity (password, MFA, SSO) is managed by Keycloak.
-- This table stores EdgePulse preferences and scope only.
-- Role is NOT stored here -- see UserRoles table below.

CREATE TABLE UserProfiles (
    Id              UNIQUEIDENTIFIER    NOT NULL
                    DEFAULT NEWSEQUENTIALID(),
    TenantId        UNIQUEIDENTIFIER    NOT NULL,
    KeycloakUserId  NVARCHAR(200)       NOT NULL,
    -- Links to Keycloak internal user UUID
    Email           NVARCHAR(300)       NOT NULL,
    FullName        NVARCHAR(300)       NOT NULL,
    -- Role column intentionally removed.
    -- Role assignment is in UserRoles table.
    -- Allows future multi-role support.
    NotifyEmail     BIT                 NOT NULL DEFAULT 1,
    NotifyInApp     BIT                 NOT NULL DEFAULT 1,
    IsActive        BIT                 NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2           NOT NULL
                    DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2           NOT NULL
                    DEFAULT GETUTCDATE(),
    IsDeleted       BIT                 NOT NULL DEFAULT 0,
    DeletedAt       DATETIME2           NULL,

    CONSTRAINT PK_UserProfiles PRIMARY KEY (Id),
    CONSTRAINT UQ_UserProfiles_KeycloakId
        UNIQUE (TenantId, KeycloakUserId)
);
```

### 3.10 Roles Table

```sql
-- Master list of all roles in EdgePulse.
-- Seeded at application startup.
-- Not created by users -- system-defined only.

CREATE TABLE Roles (
    Id          UNIQUEIDENTIFIER    NOT NULL
                DEFAULT NEWSEQUENTIALID(),
    Name        NVARCHAR(100)       NOT NULL,
    -- SuperAdmin, CustomerAdmin, MillManager,
    -- Operator, Executive
    Description NVARCHAR(500)       NULL,
    Scope       NVARCHAR(20)        NOT NULL,
    -- Scope: Platform, Tenant, Mill, Area
    IsSystem    BIT                 NOT NULL DEFAULT 1,
    -- System roles cannot be deleted by users
    CreatedAt   DATETIME2           NOT NULL
                DEFAULT GETUTCDATE(),

    CONSTRAINT PK_Roles PRIMARY KEY (Id),
    CONSTRAINT UQ_Roles_Name UNIQUE (Name)
);

-- Seed data (inserted at application startup):
-- SuperAdmin    -> Scope: Platform
-- CustomerAdmin -> Scope: Tenant
-- MillManager   -> Scope: Mill
-- Operator      -> Scope: Area
-- Executive     -> Scope: Tenant (read only)
```

### 3.11 UserRoles Table

```sql
-- Assigns a role to a user within a specific scope.
-- One user can have one role per mill.
-- Replaces the simple Role string column on UserProfiles.

CREATE TABLE UserRoles (
    Id               UNIQUEIDENTIFIER    NOT NULL
                     DEFAULT NEWSEQUENTIALID(),
    TenantId         UNIQUEIDENTIFIER    NOT NULL,
    UserProfileId    UNIQUEIDENTIFIER    NOT NULL,
    RoleId           UNIQUEIDENTIFIER    NOT NULL,
    MillId           UNIQUEIDENTIFIER    NULL,
    -- NULL for SuperAdmin and CustomerAdmin
    -- Required for MillManager, Operator, Executive
    AssignedAt       DATETIME2           NOT NULL
                     DEFAULT GETUTCDATE(),
    AssignedByUserId NVARCHAR(200)       NOT NULL,
    ExpiresAt        DATETIME2           NULL,
    -- NULL means permanent assignment
    -- Set for temporary access (e.g. consultant)
    IsActive         BIT                 NOT NULL DEFAULT 1,

    CONSTRAINT PK_UserRoles PRIMARY KEY (Id),
    CONSTRAINT FK_UserRoles_UserProfiles
        FOREIGN KEY (UserProfileId)
        REFERENCES UserProfiles(Id),
    CONSTRAINT FK_UserRoles_Roles
        FOREIGN KEY (RoleId)
        REFERENCES Roles(Id),
    CONSTRAINT UQ_UserRoles_UserRoleMill
        UNIQUE (UserProfileId, RoleId, MillId)
);
```

### 3.12 RolePermissions Table

```sql
-- Defines what each role is allowed to do.
-- Seeded at application startup.
-- Permission-based authorization:
--   check user.HasPermission("Device.Create")
--   instead of user.Role == "MillManager"

CREATE TABLE RolePermissions (
    Id          UNIQUEIDENTIFIER    NOT NULL
                DEFAULT NEWSEQUENTIALID(),
    RoleId      UNIQUEIDENTIFIER    NOT NULL,
    Permission  NVARCHAR(100)       NOT NULL,
    -- Format: Entity.Action
    -- e.g. Device.Create, Device.Read, Device.Update
    --      Alert.Acknowledge, Alert.Assign
    --      Mill.Manage, User.Invite
    --      Report.Export, AuditLog.Read

    CONSTRAINT PK_RolePermissions PRIMARY KEY (Id),
    CONSTRAINT FK_RolePermissions_Roles
        FOREIGN KEY (RoleId)
        REFERENCES Roles(Id),
    CONSTRAINT UQ_RolePermissions
        UNIQUE (RoleId, Permission)
);

-- Seed data per role:
-- SuperAdmin    -> all permissions (wildcard)
-- CustomerAdmin -> tenant + mill + device + user mgmt
-- MillManager   -> mill + device + alert management
-- Operator      -> device read + alert acknowledge only
-- Executive     -> read only across all entities
```

### 3.13 OperatorAreaAssignments Table

```sql
-- Many-to-many: one Operator can cover multiple Areas
-- within a single Mill

CREATE TABLE OperatorAreaAssignments (
    Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    TenantId        UNIQUEIDENTIFIER    NOT NULL,
    UserProfileId   UNIQUEIDENTIFIER    NOT NULL,
    AreaId          UNIQUEIDENTIFIER    NOT NULL,
    AssignedAt      DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    AssignedByUserId NVARCHAR(200)      NOT NULL,

    CONSTRAINT PK_OperatorAreaAssignments PRIMARY KEY (Id),
    CONSTRAINT FK_OpAreaAssign_UserProfiles FOREIGN KEY (UserProfileId)
        REFERENCES UserProfiles(Id),
    CONSTRAINT FK_OpAreaAssign_Areas FOREIGN KEY (AreaId)
        REFERENCES Areas(Id),
    CONSTRAINT UQ_OpAreaAssign UNIQUE (UserProfileId, AreaId)
);
```

### 3.14 Notifications Table

```sql
CREATE TABLE Notifications (
    Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    TenantId        UNIQUEIDENTIFIER    NOT NULL,
    UserId          NVARCHAR(200)       NOT NULL,  -- Keycloak userId
    AlertId         UNIQUEIDENTIFIER    NULL,
    Type            NVARCHAR(50)        NOT NULL,
    -- Type: AlertTriggered, AlertAssigned, AlertResolved, System
    Title           NVARCHAR(300)       NOT NULL,
    Message         NVARCHAR(MAX)       NOT NULL,
    IsRead          BIT                 NOT NULL DEFAULT 0,
    ReadAt          DATETIME2           NULL,
    Channel         NVARCHAR(20)        NOT NULL DEFAULT 'InApp',
    -- Channel: InApp, Email
    CreatedAt       DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_Notifications PRIMARY KEY (Id),
    CONSTRAINT FK_Notifications_Alerts FOREIGN KEY (AlertId)
        REFERENCES Alerts(Id)
);
```

### 3.15 AuditLogs Table

```sql
-- Immutable. No UPDATE or DELETE ever runs on this table.
-- Append only. Retained for 24 months.

CREATE TABLE AuditLogs (
    Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    TenantId        UNIQUEIDENTIFIER    NOT NULL,
    UserId          NVARCHAR(200)       NOT NULL,  -- Keycloak userId
    UserEmail       NVARCHAR(300)       NOT NULL,  -- denormalized
    Action          NVARCHAR(100)       NOT NULL,
    -- e.g. Device.Created, Alert.Acknowledged, User.Invited
    EntityType      NVARCHAR(100)       NOT NULL,
    -- e.g. Device, Alert, Mill, UserProfile
    EntityId        NVARCHAR(200)       NOT NULL,
    OldValuesJson   NVARCHAR(MAX)       NULL,
    -- JSON snapshot of entity before change
    NewValuesJson   NVARCHAR(MAX)       NULL,
    -- JSON snapshot of entity after change
    IpAddress       NVARCHAR(50)        NULL,
    UserAgent       NVARCHAR(500)       NULL,
    CorrelationId   NVARCHAR(100)       NULL,
    -- Links related log entries (e.g. one request = one correlationId)
    CreatedAt       DATETIME2           NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT PK_AuditLogs PRIMARY KEY (Id)
    -- No foreign keys intentionally -- audit logs must survive
    -- even if related entities are deleted
);
```

---

## 4. Configurable Lookup Tables

### 4.1 Design Philosophy

EdgePulse is designed for multiple industries beyond Pulp & Paper.
Rather than hardcoding device types, statuses, and alert severities,
all lookup values are fully configurable at three levels:

```
LEVEL 1: Industry Templates (SuperAdmin)
  Predefined sets of lookup values per industry.
  Examples: Pulp & Paper, Manufacturing, Generic.
  SuperAdmin creates and manages templates.

LEVEL 2: Tenant Configuration (CustomerAdmin)
  Each tenant is assigned one industry template.
  CustomerAdmin can:
    -> Rename template values to their vocabulary
    -> Deactivate values they don't use
    -> Add custom values specific to their org
    -> Cannot delete system template values

LEVEL 3: Well-Known GUIDs (Application Code)
  System template values have fixed predictable GUIDs.
  Referenced in code by name, not by string or integer.
  No magic strings. Full compile-time safety.
```

### 4.2 Industry Templates Table

```sql
CREATE TABLE IndustryTemplates (
    Id          UNIQUEIDENTIFIER    NOT NULL
                DEFAULT NEWSEQUENTIALID(),
    Name        NVARCHAR(200)       NOT NULL,
    -- "Pulp & Paper", "Manufacturing", "Generic"
    Description NVARCHAR(500)       NULL,
    IsDefault   BIT                 NOT NULL DEFAULT 0,
    -- IsDefault = true -> assigned when no template selected
    CreatedAt   DATETIME2           NOT NULL
                DEFAULT GETUTCDATE(),

    CONSTRAINT PK_IndustryTemplates PRIMARY KEY (Id),
    CONSTRAINT UQ_IndustryTemplates_Name UNIQUE (Name)
);

-- Seed data (inserted at startup):
-- Pulp & Paper  (IsDefault = false)
-- Manufacturing (IsDefault = false)
-- Generic       (IsDefault = true)
```

### 4.3 TenantTemplates Table

```sql
-- Assigns one industry template to one tenant.
-- Done by SuperAdmin during tenant onboarding.

CREATE TABLE TenantTemplates (
    Id          UNIQUEIDENTIFIER    NOT NULL
                DEFAULT NEWSEQUENTIALID(),
    TenantId    UNIQUEIDENTIFIER    NOT NULL,
    TemplateId  UNIQUEIDENTIFIER    NOT NULL,
    AssignedAt  DATETIME2           NOT NULL
                DEFAULT GETUTCDATE(),
    AssignedBy  NVARCHAR(200)       NOT NULL,

    CONSTRAINT PK_TenantTemplates PRIMARY KEY (Id),
    CONSTRAINT FK_TenantTemplates_Tenants
        FOREIGN KEY (TenantId) REFERENCES Tenants(Id),
    CONSTRAINT FK_TenantTemplates_Templates
        FOREIGN KEY (TemplateId)
        REFERENCES IndustryTemplates(Id),
    CONSTRAINT UQ_TenantTemplates_Tenant
        UNIQUE (TenantId)
    -- One template per tenant
);
```

### 4.4 DeviceTypes Lookup Table

```sql
-- TemplateId SET, TenantId NULL  -> template value (system)
-- TemplateId NULL, TenantId SET  -> tenant custom value

CREATE TABLE DeviceTypes (
    Id          UNIQUEIDENTIFIER    NOT NULL
                DEFAULT NEWSEQUENTIALID(),
    TemplateId  UNIQUEIDENTIFIER    NULL,
    TenantId    UNIQUEIDENTIFIER    NULL,
    Name        NVARCHAR(100)       NOT NULL,
    Code        NVARCHAR(50)        NOT NULL,
    Description NVARCHAR(300)       NULL,
    Icon        NVARCHAR(50)        NULL,
    IsSystem    BIT                 NOT NULL DEFAULT 0,
    -- System values cannot be deleted
    IsActive    BIT                 NOT NULL DEFAULT 1,
    SortOrder   INT                 NOT NULL DEFAULT 0,
    CreatedAt   DATETIME2           NOT NULL
                DEFAULT GETUTCDATE(),

    CONSTRAINT PK_DeviceTypes PRIMARY KEY (Id),
    CONSTRAINT FK_DeviceTypes_Templates
        FOREIGN KEY (TemplateId)
        REFERENCES IndustryTemplates(Id)
);

-- Seed data per template:

-- Pulp & Paper Template:
--   Pump, Motor, Valve, Digester,
--   Chip Feeder, Pulper, Refiner

-- Manufacturing Template:
--   CNC Machine, Robot Arm, Conveyor,
--   Press, Lathe, Welding Station

-- Generic Template:
--   Sensor, Controller, Actuator, Motor, Pump
```

### 4.5 DeviceStatuses Lookup Table

```sql
CREATE TABLE DeviceStatuses (
    Id          UNIQUEIDENTIFIER    NOT NULL
                DEFAULT NEWSEQUENTIALID(),
    TemplateId  UNIQUEIDENTIFIER    NULL,
    TenantId    UNIQUEIDENTIFIER    NULL,
    Name        NVARCHAR(100)       NOT NULL,
    Code        NVARCHAR(50)        NOT NULL,
    Description NVARCHAR(300)       NULL,
    Color       NVARCHAR(20)        NULL,
    -- Hex color for UI display e.g. "#22c55e"
    IsSystem    BIT                 NOT NULL DEFAULT 0,
    IsActive    BIT                 NOT NULL DEFAULT 1,
    SortOrder   INT                 NOT NULL DEFAULT 0,
    CreatedAt   DATETIME2           NOT NULL
                DEFAULT GETUTCDATE(),

    CONSTRAINT PK_DeviceStatuses PRIMARY KEY (Id),
    CONSTRAINT FK_DeviceStatuses_Templates
        FOREIGN KEY (TemplateId)
        REFERENCES IndustryTemplates(Id)
);

-- Generic Template seed (shared across all):
--   Online        (#22c55e green)
--   Offline       (#ef4444 red)
--   Maintenance   (#f59e0b amber)
--   Decommissioned (#6b7280 grey)

-- Manufacturing Template extras:
--   Setup         (#3b82f6 blue)
--   Calibrating   (#8b5cf6 purple)

-- Pulp & Paper Template extras:
--   Standby       (#06b6d4 teal)
```

### 4.6 AlertSeverities Lookup Table

```sql
CREATE TABLE AlertSeverities (
    Id          UNIQUEIDENTIFIER    NOT NULL
                DEFAULT NEWSEQUENTIALID(),
    TemplateId  UNIQUEIDENTIFIER    NULL,
    TenantId    UNIQUEIDENTIFIER    NULL,
    Name        NVARCHAR(100)       NOT NULL,
    Code        NVARCHAR(50)        NOT NULL,
    Description NVARCHAR(300)       NULL,
    Color       NVARCHAR(20)        NULL,
    Priority    INT                 NOT NULL DEFAULT 0,
    -- Lower number = higher priority
    -- Critical = 1, High = 2, Medium = 3, Low = 4
    IsSystem    BIT                 NOT NULL DEFAULT 0,
    IsActive    BIT                 NOT NULL DEFAULT 1,
    SortOrder   INT                 NOT NULL DEFAULT 0,
    CreatedAt   DATETIME2           NOT NULL
                DEFAULT GETUTCDATE(),

    CONSTRAINT PK_AlertSeverities PRIMARY KEY (Id),
    CONSTRAINT FK_AlertSeverities_Templates
        FOREIGN KEY (TemplateId)
        REFERENCES IndustryTemplates(Id)
);

-- Generic Template seed:
--   Critical (Priority 1, #ef4444)
--   High     (Priority 2, #f97316)
--   Medium   (Priority 3, #f59e0b)
--   Low      (Priority 4, #22c55e)
```

### 4.7 AlertStatuses Lookup Table

```sql
CREATE TABLE AlertStatuses (
    Id          UNIQUEIDENTIFIER    NOT NULL
                DEFAULT NEWSEQUENTIALID(),
    TemplateId  UNIQUEIDENTIFIER    NULL,
    TenantId    UNIQUEIDENTIFIER    NULL,
    Name        NVARCHAR(100)       NOT NULL,
    Code        NVARCHAR(50)        NOT NULL,
    Description NVARCHAR(300)       NULL,
    IsTerminal  BIT                 NOT NULL DEFAULT 0,
    -- IsTerminal = true means no further transitions allowed
    -- Closed and Resolved are terminal states
    IsSystem    BIT                 NOT NULL DEFAULT 0,
    IsActive    BIT                 NOT NULL DEFAULT 1,
    SortOrder   INT                 NOT NULL DEFAULT 0,
    CreatedAt   DATETIME2           NOT NULL
                DEFAULT GETUTCDATE(),

    CONSTRAINT PK_AlertStatuses PRIMARY KEY (Id),
    CONSTRAINT FK_AlertStatuses_Templates
        FOREIGN KEY (TemplateId)
        REFERENCES IndustryTemplates(Id)
);

-- Generic Template seed:
--   Open          (IsTerminal = false)
--   Acknowledged  (IsTerminal = false)
--   Assigned      (IsTerminal = false)
--   Resolved      (IsTerminal = true)
--   Closed        (IsTerminal = true)
```

### 4.8 MetricTypes Lookup Table

```sql
-- Defines what measurements a device can report.
-- Different industries have different metrics.

CREATE TABLE MetricTypes (
    Id              UNIQUEIDENTIFIER    NOT NULL
                    DEFAULT NEWSEQUENTIALID(),
    TemplateId      UNIQUEIDENTIFIER    NULL,
    TenantId        UNIQUEIDENTIFIER    NULL,
    Name            NVARCHAR(100)       NOT NULL,
    Code            NVARCHAR(50)        NOT NULL,
    DefaultUnit     NVARCHAR(20)        NOT NULL,
    -- e.g. "C", "bar", "mm/s", "L/min", "kW"
    Description     NVARCHAR(300)       NULL,
    IsSystem        BIT                 NOT NULL DEFAULT 0,
    IsActive        BIT                 NOT NULL DEFAULT 1,
    SortOrder       INT                 NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2           NOT NULL
                    DEFAULT GETUTCDATE(),

    CONSTRAINT PK_MetricTypes PRIMARY KEY (Id)
);

-- Generic Template seed:
--   Temperature   (C)
--   Pressure      (bar)
--   Vibration     (mm/s)
--   Flow Rate     (L/min)
--   Power         (kW)
--   Speed         (RPM)

-- Manufacturing Template extras:
--   Position      (mm)
--   Torque        (Nm)
--   Current       (A)
```

### 4.9 Units Lookup Table

```sql
CREATE TABLE Units (
    Id          UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    TemplateId  UNIQUEIDENTIFIER    NULL,
    TenantId    UNIQUEIDENTIFIER    NULL,
    Name        NVARCHAR(100)       NOT NULL,
    Code        NVARCHAR(50)        NOT NULL,
    Symbol      NVARCHAR(20)        NOT NULL,
    Category    NVARCHAR(100)       NOT NULL,
    Description NVARCHAR(300)       NULL,
    IsSystem    BIT                 NOT NULL DEFAULT 0,
    IsActive    BIT                 NOT NULL DEFAULT 1,
    SortOrder   INT                 NOT NULL DEFAULT 0,
    CreatedAt   DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_Units PRIMARY KEY (Id)
);
-- Seed: Celsius, Bar, PSI, mm/s, L/min, m3/h, kW, RPM
```

### 4.10 DeviceManufacturers Lookup Table

```sql
CREATE TABLE DeviceManufacturers (
    Id          UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    TemplateId  UNIQUEIDENTIFIER    NULL,
    TenantId    UNIQUEIDENTIFIER    NULL,
    Name        NVARCHAR(200)       NOT NULL,
    Code        NVARCHAR(50)        NOT NULL,
    Website     NVARCHAR(300)       NULL,
    Country     NVARCHAR(100)       NULL,
    Description NVARCHAR(300)       NULL,
    IsSystem    BIT                 NOT NULL DEFAULT 0,
    IsActive    BIT                 NOT NULL DEFAULT 1,
    SortOrder   INT                 NOT NULL DEFAULT 0,
    CreatedAt   DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_DeviceManufacturers PRIMARY KEY (Id)
);
-- Seed: ABB, Siemens, Bosch, Honeywell, Schneider Electric, Rockwell, Other
```

### 4.11 DeviceModels Lookup Table

```sql
CREATE TABLE DeviceModels (
    Id              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    TemplateId      UNIQUEIDENTIFIER    NULL,
    TenantId        UNIQUEIDENTIFIER    NULL,
    ManufacturerId  UNIQUEIDENTIFIER    NOT NULL,
    Name            NVARCHAR(200)       NOT NULL,
    Code            NVARCHAR(50)        NOT NULL,
    ModelNumber     NVARCHAR(100)       NULL,
    Specifications  NVARCHAR(MAX)       NULL,
    Description     NVARCHAR(300)       NULL,
    IsSystem        BIT                 NOT NULL DEFAULT 0,
    IsActive        BIT                 NOT NULL DEFAULT 1,
    SortOrder       INT                 NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_DeviceModels PRIMARY KEY (Id),
    CONSTRAINT FK_DeviceModels_Manufacturers
        FOREIGN KEY (ManufacturerId) REFERENCES DeviceManufacturers(Id)
);
```

### 4.12 MaintenanceTypes Lookup Table

```sql
CREATE TABLE MaintenanceTypes (
    Id          UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    TemplateId  UNIQUEIDENTIFIER    NULL,
    TenantId    UNIQUEIDENTIFIER    NULL,
    Name        NVARCHAR(100)       NOT NULL,
    Code        NVARCHAR(50)        NOT NULL,
    Color       NVARCHAR(20)        NULL,
    Description NVARCHAR(300)       NULL,
    IsSystem    BIT                 NOT NULL DEFAULT 0,
    IsActive    BIT                 NOT NULL DEFAULT 1,
    SortOrder   INT                 NOT NULL DEFAULT 0,
    CreatedAt   DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_MaintenanceTypes PRIMARY KEY (Id)
);
-- Seed: Scheduled, Corrective, Preventive, Predictive
```

### 4.13 LocationTypes Lookup Table

```sql
CREATE TABLE LocationTypes (
    Id          UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    TemplateId  UNIQUEIDENTIFIER    NULL,
    TenantId    UNIQUEIDENTIFIER    NULL,
    Name        NVARCHAR(100)       NOT NULL,
    Code        NVARCHAR(50)        NOT NULL,
    Description NVARCHAR(300)       NULL,
    IsSystem    BIT                 NOT NULL DEFAULT 0,
    IsActive    BIT                 NOT NULL DEFAULT 1,
    SortOrder   INT                 NOT NULL DEFAULT 0,
    CreatedAt   DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_LocationTypes PRIMARY KEY (Id)
);
-- Pulp & Paper seed: Building, Floor, Production Line, Section
-- Manufacturing seed: Cell, Zone, Station, Assembly Line
-- Generic seed: Area, Zone, Section
```

### 4.14 Configuration Principle

```
EDGEPULSE CONFIGURATION PRINCIPLE:

Every field presenting multiple choices to the user
must be backed by a configurable lookup table.
No dropdown in the UI has hardcoded options.
Every dropdown reads from the database.
Every lookup table has a corresponding admin screen.

EXCEPTIONS (fixed product concerns, use enums):
  UserRole       -> product architecture roles
  DeploymentMode -> infrastructure concern
  RoleScope      -> architecture concern
```

### 4.15 All Admin Configuration Screens

```
SUPERADMIN:
  Industry Templates, Device Types, Device Statuses,
  Alert Severities, Alert Statuses, Metric Types,
  Units, Manufacturers, Maintenance Types, Location Types

CUSTOMER ADMIN:
  Device Types, Device Statuses, Alert Severities,
  Alert Statuses, Metric Types, Units, Manufacturers,
  Device Models, Maintenance Types, Location Types,
  Notification Rules, User Management

MILL MANAGER:
  Device Thresholds (override per device), Area Management
```

### 4.16 TenantLookupOverrides Table

```sql
-- Tracks which template values a tenant has
-- customised or deactivated.

CREATE TABLE TenantLookupOverrides (
    Id          UNIQUEIDENTIFIER    NOT NULL
                DEFAULT NEWSEQUENTIALID(),
    TenantId    UNIQUEIDENTIFIER    NOT NULL,
    LookupType  NVARCHAR(50)        NOT NULL,
    -- "DeviceType", "DeviceStatus",
    -- "AlertSeverity", "AlertStatus", "MetricType"
    LookupId    UNIQUEIDENTIFIER    NOT NULL,
    -- ID of the template value being overridden
    DisplayName NVARCHAR(100)       NULL,
    -- Custom display name (null = use template name)
    -- NordPulp renames "Digester" -> "Kamyr Digester"
    IsActive    BIT                 NOT NULL DEFAULT 1,
    -- false = tenant has deactivated this template value
    UpdatedAt   DATETIME2           NOT NULL
                DEFAULT GETUTCDATE(),
    UpdatedBy   NVARCHAR(200)       NOT NULL,

    CONSTRAINT PK_TenantLookupOverrides PRIMARY KEY (Id),
    CONSTRAINT FK_TenantLookupOverrides_Tenants
        FOREIGN KEY (TenantId) REFERENCES Tenants(Id),
    CONSTRAINT UQ_TenantLookupOverrides
        UNIQUE (TenantId, LookupType, LookupId)
);
```

### 4.17 Well-Known GUIDs Strategy

All system template values use fixed, predictable GUIDs.
These are referenced in application code by name -- never
by string, never by integer. Full compile-time safety.

```
GUID PREFIX CONVENTION:
  00000010-xxxx = Industry Template IDs
  00000011-xxxx = Pulp & Paper DeviceType IDs
  00000012-xxxx = Pulp & Paper DeviceStatus IDs
  00000013-xxxx = Pulp & Paper AlertSeverity IDs
  00000014-xxxx = Pulp & Paper AlertStatus IDs
  00000020-xxxx = Manufacturing Template IDs
  00000021-xxxx = Manufacturing DeviceType IDs
  00000030-xxxx = Generic Template IDs
  00000031-xxxx = Generic DeviceType IDs
  00000032-xxxx = Generic DeviceStatus IDs
  00000033-xxxx = Generic AlertSeverity IDs
  00000034-xxxx = Generic AlertStatus IDs
```

### 4.18 How Lookups Are Fetched Per Tenant

```
When NordPulp requests device types:

1. Find NordPulp tenant template -> Pulp & Paper
2. Fetch all DeviceTypes from Pulp & Paper template
3. Apply TenantLookupOverrides:
     - Deactivated items removed
     - Renamed items use override DisplayName
4. Append NordPulp custom DeviceTypes (TenantId set)
5. Return merged, ordered list

Result:
  Pump, Motor, Valve,
  Kamyr Digester (renamed),   <- template value, renamed
  Chip Feeder, Refiner,       <- template values
  Black Liquor Evaporator     <- NordPulp custom
  -- Pulper excluded          <- deactivated by NordPulp
```

### 4.19 Admin UI Capabilities

```
SUPERADMIN:
  -> Manage Industry Templates
  -> Add/edit/delete template lookup values
  -> Assign templates to tenants

CUSTOMER ADMIN (Settings page):
  -> View all lookup values from their template
  -> Rename any template value
  -> Deactivate/reactivate template values
  -> Add custom values (tenant-specific)
  -> Edit/delete their own custom values
  -> Cannot delete system template values

MILL MANAGER:
  -> View lookups (read only)
  -> Uses them when registering devices
```

---

## 4b. Multimedia & File Attachments

### Design Philosophy

Files are not stored as columns on entities.
A single generic Attachments table links to any entity.
This allows unlimited files per entity and consistent
handling across the entire platform.

```
SUPPORTED ENTITIES FOR ATTACHMENTS:
  Device              -> Photo, Manual, Schematic, Datasheet
  DeviceModel         -> Model image, Datasheet
  DeviceManufacturer  -> Company logo
  Mill                -> Site photo, Floor plan
  Area                -> Layout diagram, Floor plan
  Alert               -> Fault photo, Video
  MaintenanceRecord   -> Before/after photos, Report
  UserProfile         -> Profile photo
  Tenant              -> Company logo
  IndustryTemplate    -> Template thumbnail
```

### Storage Architecture

```
CLOUD MODE:
  Azure Blob Storage
  Path: /{tenantId}/{entityType}/{entityId}/{storedFileName}
  Access: SAS tokens (temporary signed URLs, never direct)
  CDN: optional for frequently accessed files

ON-PREMISE MODE:
  MinIO (open source S3-compatible object storage)
  Runs in Docker container
  Same API as Azure Blob Storage
  Path structure identical to cloud mode

ABSTRACTION:
  IFileStorageService interface
    -> AzureBlobStorageService  (cloud)
    -> MinioStorageService      (on-premise)
  DEPLOYMENT_MODE switches automatically
```

### Attachments Table

```sql
CREATE TABLE Attachments (
    Id              UNIQUEIDENTIFIER    NOT NULL
                    DEFAULT NEWSEQUENTIALID(),
    TenantId        UNIQUEIDENTIFIER    NOT NULL,
    EntityType      NVARCHAR(100)       NOT NULL,
    -- "Device", "Alert", "Mill", "Area",
    -- "DeviceModel", "MaintenanceRecord" etc.
    EntityId        UNIQUEIDENTIFIER    NOT NULL,
    FileName        NVARCHAR(300)       NOT NULL,
    -- original filename uploaded by user
    StoredFileName  NVARCHAR(300)       NOT NULL,
    -- actual filename in storage (UUID based, prevents collisions)
    FileSize        BIGINT              NOT NULL,
    -- file size in bytes
    ContentType     NVARCHAR(100)       NOT NULL,
    -- MIME type: "image/jpeg", "application/pdf", "video/mp4"
    FileCategory    NVARCHAR(50)        NOT NULL,
    -- "Photo", "Manual", "Schematic", "Report",
    -- "Video", "FloorPlan", "Logo", "Other"
    StoragePath     NVARCHAR(500)       NOT NULL,
    -- full path in blob/MinIO storage
    IsPublic        BIT                 NOT NULL DEFAULT 0,
    -- false = requires auth + SAS token to access
    DisplayOrder    INT                 NOT NULL DEFAULT 0,
    -- for ordering multiple images on same entity
    UploadedBy      NVARCHAR(200)       NOT NULL,
    -- Keycloak userId
    UploadedAt      DATETIME2           NOT NULL
                    DEFAULT GETUTCDATE(),
    IsDeleted       BIT                 NOT NULL DEFAULT 0,
    DeletedAt       DATETIME2           NULL,

    CONSTRAINT PK_Attachments PRIMARY KEY (Id),
    CONSTRAINT FK_Attachments_Tenants
        FOREIGN KEY (TenantId) REFERENCES Tenants(Id)
);

CREATE INDEX IX_Attachments_Entity
    ON Attachments(TenantId, EntityType, EntityId)
    WHERE IsDeleted = 0;
```

### AttachmentSettings Table

```sql
-- Configurable per tenant per entity type.
-- CustomerAdmin sets limits in Settings screen.

CREATE TABLE AttachmentSettings (
    Id              UNIQUEIDENTIFIER    NOT NULL
                    DEFAULT NEWSEQUENTIALID(),
    TenantId        UNIQUEIDENTIFIER    NOT NULL,
    EntityType      NVARCHAR(100)       NOT NULL,
    MaxFileSizeMb   INT                 NOT NULL DEFAULT 10,
    MaxFileCount    INT                 NOT NULL DEFAULT 10,
    AllowedTypes    NVARCHAR(500)       NOT NULL
                    DEFAULT 'jpg,jpeg,png,pdf',
    UpdatedAt       DATETIME2           NOT NULL
                    DEFAULT GETUTCDATE(),

    CONSTRAINT PK_AttachmentSettings PRIMARY KEY (Id),
    CONSTRAINT UQ_AttachmentSettings
        UNIQUE (TenantId, EntityType)
);
```

### File Size Limits

```
CONFIGURABLE PER TENANT:
  Max file size per upload    default: 10MB
  Allowed file types          default: jpg, png, pdf, mp4
  Max files per entity        default: 10

HARD SYSTEM LIMITS:
  Max file size: 100MB (enforced at API level)
  Max files per entity: 50
```

### Admin Configuration Screen

```
CustomerAdmin -> Settings -> Attachment Settings

+================================================+
|  Attachment Settings                           |
+================================================+
|  Entity Type    | Max Size | Max Files | Types |
|-----------------|----------|-----------|-------|
|  Device         | 10MB     | 10        | Edit  |
|  Alert          | 50MB     | 20        | Edit  |
|  Mill           | 10MB     | 5         | Edit  |
|  Area           | 10MB     | 5         | Edit  |
+================================================+
```

---

## 5. Entity Relationship Diagram

```
+-------------------+       +------------------+
| IndustryTemplates |1-----*| TenantTemplates  |
+-------------------+       +--------+---------+
         |1                          |*
         |                    +------+------+
         |*                   |   Tenants   |
+---------------------+       +------+------+
| DeviceTypes         |              |1
| DeviceStatuses      |       +------+------+       +----------+
| AlertSeverities     |       |    Mills    |1-----*|  Areas   |
| AlertStatuses       |       +------+------+       +-----+----+
| MetricTypes         |              |1                   |1
+---------------------+       +------+------+       +-----+------+
         |                    |  Devices    |       |AlertThresh |
         |*                   +------+------+       |olds        |
+---------------------+              |1             +------------+
| TenantLookupOverrides              |
+---------------------+    +---------+----------+
                            |                   |
                        +---+---+   +-----------+--+
                        |Alerts |   | DeviceApiKeys|
                        +---+---+   +--------------+
                            |1
                        +---+----------+
                        |AlertAssign   |
                        |ments         |
                        +--------------+

+-------------+       +--------------+
| UserProfiles|1-----*|  UserRoles   |*-----1 Roles 1-----* RolePermissions
+------+------+       +--------------+
       |1
       |*
+------+-------------------+
| OperatorAreaAssignments  |
+--------------------------+

+------------------+   (no FK -- immutable)
|   AuditLogs      |
+------------------+

+------------------+   (Cosmos DB -- separate database)
| TelemetryReading |
| partition:deviceId
+------------------+
```

---

## 6. Cosmos DB -- Telemetry Design

### 6.1 Container Configuration

```
Database name  : edgepulse-telemetry
Container name : readings
Partition key  : /deviceId
Throughput     : Autoscale 400-4000 RU/s
TTL            : 31,536,000 seconds (12 months)
                 Documents auto-expire -- no manual cleanup
```

### 6.2 Why deviceId as Partition Key

```
Query pattern: "give me all readings for PUMP-LW-001
                between 14:00 and 15:00 today"

With deviceId as partition key:
  All readings for PUMP-LW-001 are on ONE partition.
  Query touches exactly one partition shard.
  Fast. Cheap. Scales linearly with devices.

With tenantId as partition key (wrong choice):
  All readings for ALL devices of NordPulp on one partition.
  One tenant's high volume affects partition performance.
  Hot partition problem.

With timestamp as partition key (wrong choice):
  All readings at 14:30 on one partition.
  Massive hot partition during peak hours.
  Terrible for time-range queries.
```

### 6.3 Telemetry Document Schema

```json
{
  "id"        : "uuid-v4-unique-per-reading",
  "deviceId"  : "PUMP-LW-001",
  "tenantId"  : "tenant_nordpulp",
  "millId"    : "mill_lakewood",
  "areaId"    : "area_lw_pm1",
  "deviceName": "Feed Pump 1",
  "deviceType": "Pump",
  "timestamp" : "2026-05-13T14:30:45Z",
  "readings"  : [
    {
      "metric" : "temperature",
      "value"  : 78.5,
      "unit"   : "C",
      "status" : "normal"
    },
    {
      "metric" : "inlet_pressure",
      "value"  : 3.2,
      "unit"   : "bar",
      "status" : "normal"
    },
    {
      "metric" : "flow_rate",
      "value"  : 118.0,
      "unit"   : "L/min",
      "status" : "normal"
    },
    {
      "metric" : "vibration",
      "value"  : 1.2,
      "unit"   : "mm/s",
      "status" : "warning"
    },
    {
      "metric" : "power",
      "value"  : 15.8,
      "unit"   : "kW",
      "status" : "normal"
    }
  ],
  "source"    : "edge-agent-v2",
  "_ts"       : 1715607045,
  "ttl"       : 31536000
}
```

### 6.4 Denormalization Strategy

Notice `deviceName`, `millId`, `areaId`, `deviceType` are stored
in EVERY telemetry document even though they exist in Azure SQL.

This is intentional denormalization:

```
Why denormalize:
  Cosmos DB has no JOINs.
  Dashboard shows: "PUMP-LW-001 in Paper Machine 1,
                    Lakewood Mill -- temperature 78.5C"
  Without denormalization: must query Cosmos + Azure SQL
                            for every reading displayed.
  With denormalization: one Cosmos query gives everything.

Trade-off:
  If device is renamed in Azure SQL,
  historical telemetry still shows old name.
  Acceptable -- historical accuracy is more important
  than name consistency for time-series data.
```

### 6.5 Common Queries

```sql
-- Last 24 hours for one device
SELECT * FROM readings r
WHERE r.deviceId = 'PUMP-LW-001'
AND r.timestamp >= '2026-05-12T14:30:00Z'
AND r.timestamp <= '2026-05-13T14:30:00Z'
ORDER BY r.timestamp DESC

-- Latest reading per device (dashboard status)
SELECT TOP 1 * FROM readings r
WHERE r.deviceId = 'PUMP-LW-001'
ORDER BY r.timestamp DESC

-- All devices in a mill (cross-device query)
-- NOTE: this crosses partitions -- use sparingly
SELECT * FROM readings r
WHERE r.millId = 'mill_lakewood'
AND r.timestamp >= '2026-05-13T00:00:00Z'
```

---

## 7. PostgreSQL -- Keycloak Schema

PostgreSQL is used exclusively by Keycloak.
EdgePulse application code never connects to it directly.

```
Managed by Keycloak automatically:
  REALM_ENTITY        -- EdgePulse realm config
  USER_ENTITY         -- Keycloak user accounts
  CREDENTIAL          -- Hashed passwords, MFA config
  USER_ROLE_MAPPING   -- User to role assignments
  CLIENT              -- Registered clients (Dashboard, API)
  IDENTITY_PROVIDER   -- Azure AD, on-premise AD config
  USER_SESSION        -- Active login sessions
  OFFLINE_CLIENT_SESSION -- Refresh token sessions

Do NOT manually modify these tables.
Use Keycloak Admin Console or Admin REST API only.
```

---

## 8. On-Premise Database Design

When deployed on-premise (no internet), Azure SQL and Cosmos DB
are replaced with open source equivalents.
Schema and data models remain identical.

### 8.1 SQL Server (replaces Azure SQL)

```
Same schema as Azure SQL.
SQL Server 2022 runs in Docker container.
EF Core 9 works identically with both.
Connection string is the only difference.

Cloud:     Server=tcp:edgepulse.database.windows.net,...
On-premise: Server=sqlserver,1433;Database=EdgePulse,...
```

### 8.2 MongoDB (replaces Cosmos DB)

```
Same document schema as Cosmos DB.
MongoDB 7.0 runs in Docker container.

Collection: telemetry_readings
Index: { deviceId: 1, timestamp: -1 }
-- compound index for fast device+time queries

TTL Index: { timestamp: 1 }, expireAfterSeconds: 31536000
-- auto-expire documents after 12 months
-- mirrors Cosmos DB TTL behaviour

Sharding (for scale):
  Shard key: tenantId + deviceId
  Allows horizontal scaling as data grows
```

---

## 9. Tenant Isolation Strategy

Every table in Azure SQL has a TenantId column.
Isolation is enforced at the EF Core level using
Global Query Filters -- not left to individual developers.

### 9.1 EF Core Global Query Filter

```csharp
// In DbContext -- applied automatically to every query

public class EdgePulseDbContext : DbContext
{
    private readonly string _tenantId;

    public EdgePulseDbContext(
        DbContextOptions options,
        ITenantContext tenantContext) : base(options)
    {
        _tenantId = tenantContext.TenantId;
    }

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        // Applied to every query automatically
        // Developer cannot forget this filter
        modelBuilder.Entity<Device>()
            .HasQueryFilter(d =>
                d.TenantId == _tenantId &&
                !d.IsDeleted);

        modelBuilder.Entity<Mill>()
            .HasQueryFilter(m =>
                m.TenantId == _tenantId &&
                !m.IsDeleted);

        modelBuilder.Entity<Alert>()
            .HasQueryFilter(a =>
                a.TenantId == _tenantId &&
                !a.IsDeleted);

        // Applied to all tenant-scoped entities
    }
}
```

### 9.2 What This Means In Practice

```
Developer writes:
  var devices = await _context.Devices.ToListAsync();

EF Core executes:
  SELECT * FROM Devices
  WHERE TenantId = 'tenant_nordpulp'  -- auto-injected
  AND IsDeleted = 0                    -- auto-injected

Cross-tenant query is impossible at ORM level.
Even a bug in application code cannot leak data.
Security is enforced by infrastructure, not discipline.
```

### 9.3 SuperAdmin Bypass

SuperAdmin needs to query across tenants.
This uses a separate DbContext with no global filters:

```csharp
// SuperAdmin DbContext -- bypasses tenant filter
public class SuperAdminDbContext : DbContext
{
    // No global query filters
    // Only used for platform-level operations
    // Role check enforced at API level before reaching here
}
```

---

## 10. Soft Delete Strategy

No entity is ever permanently deleted in EdgePulse.
Deletion sets IsDeleted = true and records DeletedAt timestamp.

### 10.1 Why Soft Delete

```
Audit compliance:
  If a device is deleted, its historical alerts
  and telemetry must still be queryable.
  Hard delete would break audit trail.

Recovery:
  Mill Manager accidentally deletes a device?
  SuperAdmin can restore it.
  Hard delete is unrecoverable.

Referential integrity:
  Alert references a DeviceId.
  If device is hard-deleted, alert loses its reference.
  Soft delete keeps the reference valid.
```

### 10.2 Soft Delete Rules

```
+-------------------+------------------+--------------------+
| Entity            | Soft Delete      | Cascade Effect     |
+-------------------+------------------+--------------------+
| Tenant            | Yes              | Soft delete all    |
|                   |                  | mills, areas,      |
|                   |                  | devices under it   |
+-------------------+------------------+--------------------+
| Mill              | Yes              | Soft delete all    |
|                   |                  | areas and devices  |
+-------------------+------------------+--------------------+
| Area              | Yes              | Soft delete all    |
|                   |                  | devices in area    |
+-------------------+------------------+--------------------+
| Device            | Yes              | Revoke API keys    |
|                   |                  | Keep alerts and    |
|                   |                  | telemetry history  |
+-------------------+------------------+--------------------+
| UserProfile       | Yes              | Revoke sessions    |
|                   |                  | in Keycloak        |
+-------------------+------------------+--------------------+
| Alert             | Yes (admin only) | Keep assignments   |
|                   |                  | and notifications  |
+-------------------+------------------+--------------------+
| AuditLog          | NEVER            | Immutable always   |
+-------------------+------------------+--------------------+
| Notification      | No               | Hard delete after  |
|                   |                  | 90 days (cleanup)  |
+-------------------+------------------+--------------------+
```

---

## 11. Audit Log Design

Every significant action in EdgePulse is recorded
in the AuditLogs table. It is append-only and immutable.

### 11.1 Actions Logged

```
AUTHENTICATION:
  Auth.Login          Auth.Logout       Auth.LoginFailed

DEVICE MANAGEMENT:
  Device.Created      Device.Updated    Device.Deleted
  Device.Restored     Device.StatusChanged

ORGANISATION:
  Tenant.Created      Mill.Created      Area.Created
  Tenant.Suspended    Mill.Deleted      Area.Deleted

ALERTS:
  Alert.Triggered     Alert.Acknowledged
  Alert.Assigned      Alert.Resolved    Alert.Closed

USER MANAGEMENT:
  User.Invited        User.RoleChanged
  User.Deactivated    User.Restored

API KEYS:
  ApiKey.Generated    ApiKey.Revoked
```

### 11.2 Audit Log Example Records

```json
{
  "id"            : "uuid",
  "tenantId"      : "tenant_nordpulp",
  "userId"        : "keycloak-user-uuid",
  "userEmail"     : "manager.lakewood@nordpulp.com",
  "action"        : "Device.Created",
  "entityType"    : "Device",
  "entityId"      : "device-uuid",
  "oldValuesJson" : null,
  "newValuesJson" : {
    "code"   : "PUMP-LW-001",
    "name"   : "Feed Pump 1",
    "type"   : "Pump",
    "areaId" : "area-uuid",
    "status" : "Offline"
  },
  "ipAddress"     : "10.0.1.45",
  "correlationId" : "req-uuid",
  "createdAt"     : "2026-05-13T14:30:45Z"
}
```

### 11.3 Future: Event Sourcing Upgrade Path

```
Current design: AuditLog table (simple, effective)

Future upgrade: Event Sourcing
  Every state change stored as immutable domain event.
  Current state = replay of all events from beginning.
  Benefits: full history replay, temporal queries,
            event-driven integrations.

Migration path:
  AuditLog entries become the first event store.
  Gradually introduce domain events alongside.
  EventStore table added, AuditLog becomes a projection.

This is documented here so architects understand
the evolution path without needing a rewrite.
```

---

## 12. Read Replica Strategy

Azure SQL supports read replicas for separating
read-heavy report queries from write operations.

### 12.1 Connection String Strategy

```csharp
// Two DbContext registrations in DI

// Write DbContext -- all INSERT, UPDATE, DELETE
// Points to primary Azure SQL instance
services.AddDbContext<EdgePulseWriteDbContext>(opts =>
    opts.UseSqlServer(config["Sql:PrimaryConnection"]));

// Read DbContext -- all SELECT for reports/dashboard
// Points to read replica (Azure SQL geo-replica)
services.AddDbContext<EdgePulseReadDbContext>(opts =>
    opts.UseSqlServer(config["Sql:ReadReplicaConnection"])
        .UseQueryTrackingBehavior(
            QueryTrackingBehavior.NoTracking));
```

### 12.2 CQRS Split

```
Commands (write operations):
  RegisterDevice    -> WriteDbContext -> Primary SQL
  CreateAlert       -> WriteDbContext -> Primary SQL
  AcknowledgeAlert  -> WriteDbContext -> Primary SQL

Queries (read operations):
  GetDeviceList     -> ReadDbContext  -> Read Replica
  GetMillReport     -> ReadDbContext  -> Read Replica
  GetAlertHistory   -> ReadDbContext  -> Read Replica
  GetDashboardData  -> ReadDbContext  -> Read Replica
```

### 12.3 Read Replica Lag

```
Azure SQL read replica has ~5 second replication lag.
This is acceptable for EdgePulse because:

  Dashboard refresh interval: 30 seconds
  Report data: historical (not real-time)
  Alert list: updated every 10 seconds

  A 5-second lag is imperceptible to users.

For real-time data (device status, live telemetry):
  Read from Cosmos DB directly (no replica lag).
  Cosmos DB is source of truth for live readings.
```

---

## 13. Telemetry Retention Strategy

### 13.1 Phase 1 -- TTL Auto-Expire (Implemented)

```
Cosmos DB TTL: 31,536,000 seconds = 12 months
MongoDB TTL index: expireAfterSeconds: 31,536,000

Documents auto-expire after 12 months.
No manual cleanup jobs needed.
Storage cost stays bounded.
```

### 13.2 Phase 2 -- Archive to Cold Storage (Future)

```
Before TTL expires, archive to cold storage:

Cloud:    Azure Blob Storage (Cool tier)
          Cost: ~$0.01/GB/month vs Cosmos $0.25/GB/month
          25x cheaper for historical data

On-premise: Local NAS or object storage (MinIO)

Archive trigger:
  Azure Function runs monthly
  Queries Cosmos for documents older than 11 months
  Serializes to JSON/Parquet files
  Uploads to Blob Storage with path:
    /archive/{tenantId}/{year}/{month}/{deviceId}.json
  Deletes from Cosmos after confirmed upload

Retrieval:
  Dashboard shows: "Data older than 12 months is archived"
  User clicks "Load archived data"
  API fetches from Blob Storage on demand
  Slightly slower but functional
```

### 13.3 Retention Summary

```
+------------------+------------------+------------------+
| Data Age         | Location         | Access Speed     |
+------------------+------------------+------------------+
| 0 - 12 months    | Cosmos DB        | Fast (<100ms)    |
| 12 - 36 months   | Azure Blob       | Slow (1-5s)      |
| > 36 months      | Deleted          | N/A              |
+------------------+------------------+------------------+
```

---

## 14. Data Flow Diagrams

### 14.1 Device Registration Flow

```
Client (Mill Manager)
  |
  | POST /api/devices
  | { name, code, type, areaId, ... }
  |
  v
Device API
  |
  | 1. Validate JWT -- extract tenantId, millId
  | 2. Validate payload (FluentValidation)
  | 3. Check area belongs to tenant mill
  | 4. Check device code unique within tenant
  | 5. Generate device API key (UUID v4)
  | 6. Hash API key (SHA-256)
  |
  | BEGIN TRANSACTION
  | 7. INSERT INTO Devices (...)
  | 8. INSERT INTO DeviceApiKeys (KeyHash, KeyPrefix)
  | 9. INSERT INTO AuditLogs (Device.Created)
  | COMMIT TRANSACTION
  |
  | 10. Return 201 Created
  |     { deviceId, code, apiKey (plain text -- shown once) }
  v
Client stores API key securely
Programs device/edge agent with API key
```

### 14.2 Telemetry Flow

```
Device (PUMP-LW-001)
  |
  | POST /api/telemetry/ingest
  | X-Device-Api-Key: dev_a1b2xxxx
  | { deviceId, readings: [...], timestamp }
  |
  v
Telemetry Service (NestJS)
  |
  | 1. Extract API key from header
  | 2. Check in-memory cache (5 min TTL)
  |    Cache HIT  -> use cached device info
  |    Cache MISS -> call Device API /internal/validate-key
  |                  cache result for 5 min
  | 3. Validate payload schema
  | 4. Enrich: add tenantId, millId, areaId from cache
  | 5. Publish to Service Bus queue
  | 6. Return 202 Accepted immediately
  |
  v
Azure Service Bus Queue (telemetry-ingest)
  |
  | Message held until Processor consumes it
  | At-least-once delivery guaranteed
  |
  v
Processor Service (.NET 9 Worker)
  |
  | 1. Receive message from queue
  | 2. Deserialize telemetry message
  | 3. INSERT into Cosmos DB (store raw reading)
  | 4. Load AlertThresholds for device from Azure SQL
  |    (cached in memory, refreshed every 60 seconds)
  | 5. For each metric in reading:
  |    a. Check if value breaches threshold
  |    b. Track consecutive breach count per device/metric
  |       (in-memory counter, reset on normal reading)
  |    c. If count >= 3 (ConsecutiveCount):
  |       -> Call Azure OpenAI for alert summary
  |       -> INSERT into Alerts table
  |       -> INSERT into Notifications for affected users
  |       -> Send email via SendGrid (Critical + High only)
  |       -> Reset consecutive counter
  | 6. Complete (acknowledge) Service Bus message
  |
  v
Alert visible on dashboard within 30 seconds
```

### 14.3 Alert Acknowledgement Flow

```
Operator (assigned to area)
  |
  | PUT /api/alerts/{alertId}/acknowledge
  | Authorization: Bearer <jwt>
  |
  v
Device API
  |
  | 1. Validate JWT
  | 2. Check alert belongs to tenant (global filter)
  | 3. Check operator has access to alert's area
  | 4. Check alert is in Open or Assigned status
  |
  | BEGIN TRANSACTION
  | 5. UPDATE Alerts SET
  |       Status = 'Acknowledged',
  |       AcknowledgedAt = GETUTCDATE(),
  |       AcknowledgedBy = userId
  | 6. INSERT INTO AuditLogs (Alert.Acknowledged)
  | COMMIT TRANSACTION
  |
  | 7. Return 200 OK
  v
Alert status updated on all connected dashboards
```

---

## 15. Indexing Strategy

### 15.1 Azure SQL Indexes

```sql
-- Devices -- most common query patterns
CREATE INDEX IX_Devices_TenantId_AreaId
    ON Devices(TenantId, AreaId)
    WHERE IsDeleted = 0;

CREATE INDEX IX_Devices_TenantId_Status
    ON Devices(TenantId, Status)
    WHERE IsDeleted = 0;

-- Alerts -- dashboard and report queries
CREATE INDEX IX_Alerts_TenantId_Status
    ON Alerts(TenantId, Status, TriggeredAt DESC)
    WHERE IsDeleted = 0;

CREATE INDEX IX_Alerts_DeviceId_TriggeredAt
    ON Alerts(DeviceId, TriggeredAt DESC);

-- AuditLogs -- compliance queries
CREATE INDEX IX_AuditLogs_TenantId_CreatedAt
    ON AuditLogs(TenantId, CreatedAt DESC);

CREATE INDEX IX_AuditLogs_EntityType_EntityId
    ON AuditLogs(EntityType, EntityId, CreatedAt DESC);

-- DeviceApiKeys -- telemetry auth (hot path)
CREATE INDEX IX_DeviceApiKeys_KeyHash
    ON DeviceApiKeys(KeyHash)
    WHERE IsActive = 1;

-- Notifications -- unread count (frequent query)
CREATE INDEX IX_Notifications_UserId_IsRead
    ON Notifications(UserId, IsRead, CreatedAt DESC);
```

### 15.2 Cosmos DB Indexes

```
Cosmos DB indexes all properties by default.
We override to optimise for our query patterns:

Include:
  /deviceId/*     -- partition key, always indexed
  /timestamp/?    -- time-range queries
  /tenantId/?     -- tenant filter
  /millId/?       -- mill-level dashboard queries

Exclude (save RU cost):
  /readings/*     -- large array, never queried directly
  /aiSummary/?    -- long text, never queried
  /source/?       -- metadata, never queried

Composite index:
  ["/deviceId ASC", "/timestamp DESC"]
  -- optimises the most common query:
  -- "all readings for device X ordered by time"
```

---

## 16. Scalability Decisions

### 16.1 Azure SQL Scalability

```
Current: Standard S3 (100 DTUs)
  Handles: ~200 concurrent users, normal query load

Scale trigger: CPU > 80% sustained for 10 minutes
Scale to: Standard S4 (200 DTUs) -- vertical scale

Read replica:
  Offloads all report and dashboard queries
  Primary only handles writes and real-time lookups
  Effectively doubles read capacity

Future -- horizontal sharding:
  If single tenant grows beyond SQL limits:
  Shard by tenantId across multiple SQL instances
  Each instance owns a subset of tenants
  Application routes to correct shard via tenant lookup
```

### 16.2 Cosmos DB Scalability

```
Current: Autoscale 400-4000 RU/s per container
  Handles: ~1000 messages/minute per tenant comfortably

Scale automatically:
  Cosmos DB autoscale adjusts RU/s based on load
  No manual intervention needed

Partition growth:
  Each deviceId is one logical partition
  Cosmos DB distributes partitions across physical nodes
  Adding more devices = more partitions = more capacity
  Linear scalability with device count

Cross-partition queries (avoid):
  Queries by millId or tenantId cross partitions
  Use for reports only, not real-time dashboard
  Cache report results in Redis (future)
```

### 16.3 Future Redis Caching Layer

```
Add Redis between Device API and Azure SQL
for frequently read, rarely changing data:

Cache candidates:
  Device list per area (TTL: 60s)
  AlertThreshold config per device (TTL: 60s)
  Mill and Area metadata (TTL: 5 min)
  User profile and permissions (TTL: 5 min)
  Dashboard summary counts (TTL: 30s)

Not cached:
  Alert list (must be real-time)
  Telemetry (already in Cosmos DB, fast enough)
  Audit logs (compliance, must be direct)

Implementation:
  IDeviceCache interface
  RedisDeviceCache (cloud)
  InMemoryDeviceCache (on-premise, no Redis needed)
```

---

*Document ends. Next: 04-api-design.md*
