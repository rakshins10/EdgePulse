# EdgePulse -- Architecture Document

**Version:** 1.0
**Status:** Approved
**Last Updated:** May 2026
**Author:** Rakshith N S
**Depends On:** 01-requirements.md v1.0

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [System Context Diagram](#2-system-context-diagram)
3. [Component Architecture](#3-component-architecture)
4. [Service Descriptions](#4-service-descriptions)
5. [Communication Patterns](#5-communication-patterns)
6. [Authentication & Authorization Flow](#6-authentication--authorization-flow)
7. [Telemetry Pipeline](#7-telemetry-pipeline)
8. [Data Architecture](#8-data-architecture)
9. [Deployment Architecture](#9-deployment-architecture)
10. [Local Development Architecture](#10-local-development-architecture)
11. [On-Premise Deployment with HAProxy](#11-on-premise-deployment-with-haproxy)
12. [Architecture Decision Records (ADRs)](#12-architecture-decision-records-adrs)
13. [Non-Functional Architecture](#13-non-functional-architecture)
14. [Security Architecture](#14-security-architecture)

---

## 1. Architecture Overview

EdgePulse follows a **microservices architecture** with three backend services,
one identity provider, one frontend, and an API Gateway as the single entry
point for all client traffic.

### Core Principles

```
1. Single Responsibility
   Each service owns one domain and does it well.
   Device API owns devices. Telemetry Service owns ingestion.
   Processor owns anomaly detection.

2. Async by Default for High Volume
   Telemetry pipeline is fully asynchronous via Service Bus.
   No service blocks waiting for another during ingestion.

3. Security at Every Layer
   JWT validation at gateway. Tenant isolation at data layer.
   Zero hardcoded secrets. Managed Identity everywhere.

4. Observable by Design
   Every service emits structured logs, metrics, and traces
   to App Insights from day one. Not added later.

5. Environment Parity
   Local Docker Compose mirrors production Azure topology.
   What runs locally runs in Azure unchanged.
```

---

## 2. System Context Diagram

Who uses EdgePulse and how they interact with it from the outside:

```
                        +----------------------------------+
                        |        NORDPULP INDUSTRIES       |
                        |                                  |
  +------------+        |  +----------+  +-----------+     |
  | EdgePulse  |        |  | Customer |  |   Mill    |     |
  | SuperAdmin |        |  |  Admin   |  |  Manager  |     |
  +-----+------+        |  +----+-----+  +-----+-----+     |
        |               |       |              |           |
        |               |  +-----------+  +-----------+    |
        |               |  | Operator  |  | Executive |    |
        |               |  +-----------+  +-----------+    |
        |               +----------------------------------+
        |                         |
        |               All users access via
        |               Web Browser (HTTPS)
        |                         |
        v                         v
+-----------------------------------------------+
|              EDGEPULSE PLATFORM               |
|                                               |
|  Single entry point: Azure API Management     |
|  Identity: Keycloak + Azure AD SSO            |
|                                               |
+-----------------------------------------------+
        ^
        |
        | REST API + API Key (HTTPS)
        |
+-----------------------------------------------+
|            INDUSTRIAL DEVICES                 |
|                                               |
|  PUMP-LW-001, MOTOR-LW-001, SENSOR-RV-001     |
|  (via Edge Agent or direct REST calls)        |
+-----------------------------------------------+
```

---

## 3. Component Architecture

Full internal view of all services and how they connect:

```
+=====================================================================+
|                      EDGEPULSE PLATFORM                             |
|                                                                     |
|  +---------------------+     +------------------------------+       |
|  |   React Dashboard   |     |  Keycloak Identity Provider  |       |
|  |   (TypeScript)      |<--->|  + PostgreSQL 16             |       |
|  |   Azure Static      |     |  + Azure AD SSO (OIDC)       |       |
|  |   Web Apps          |     |  + On-premise AD (LDAP)      |       |
|  +----------+----------+     +------------------------------+       |
|             |                           |                           |
|             | HTTPS                     | JWT tokens                |
|             v                           v                           |
|  +-------------------------------------------------------------+    |
|  |              Azure API Management (APIM)                    |    |
|  |              -- Single entry point for all clients  --      |    |
|  |              -- JWT validation                      --      |    |
|  |              -- Rate limiting per tenant            --      |    |
|  |              -- Request routing                     --      |    |
|  +--------+---------------------------+--------------------+---+    |
|           |                           |                    |        |
|           | /api/devices              | /api/telemetry     |        |
|           | /api/mills                | /api/ingest        |        |
|           | /api/alerts               |                    |        |
|           v                           v                    |        |
|  +------------------+    +--------------------+            |        |
|  |   Device API     |    | Telemetry Service  |            |        |
|  |   (.NET 9)       |    | (Node.js / NestJS) |            |        |
|  |                  |    |                    |            |        |
|  | - Device CRUD    |    | - Receive readings |            |        |
|  | - Mill/Area mgmt |    | - Validate API key |            |        |
|  | - Alert config   |    | - Validate payload |            |        |
|  | - User mgmt      |    | - Publish to queue |            |        |
|  | - Audit logs     |    |                    |            |        |
|  | - Reports        |    +--------+-----------+            |        |
|  |                  |             |                        |        |
|  +--------+---------+             | publish                |        |
|           |                       v                        |        |
|           |            +--------------------+              |        |
|           |            | Azure Service Bus  |              |        |
|           |            | Queue              |              |        |
|           |            | (telemetry-ingest) |              |        |
|           |            +--------+-----------+              |        |
|           |                     |                          |        |
|           |                     | consume                  |        |
|           |                     v                          |        |
|           |            +--------------------+              |        |
|           |            | Processor Service  |              |        |
|           |            | (.NET 9 Worker)    |              |        |
|           |            |                    |              |        |
|           |            | - Read from queue  |              |        |
|           |            | - Check thresholds |              |        |
|           |            | - Detect anomalies |              |        |
|           |            | - Azure OpenAI     |              |        |
|           |            | - Create alerts    |              |        |
|           |            | - Send emails      |              |        |
|           |            +----+----------+----+              |        |
|           |                 |          |                   |        |
|           v                 v          v                   |        |
|  +---------------+  +------------+  +------------------+   |        |
|  | Azure SQL     |  | Cosmos DB  |  | Azure OpenAI     |   |        |
|  | (devices,     |  | (telemetry |  | (GPT-4o-mini)    |   |        |
|  |  users,       |  |  readings) |  | alert summaries  |   |        |
|  |  alerts,      |  +------------+  +------------------+   |        |
|  |  audit logs)  |                                         |        |
|  +---------------+                                         |        |
|                                                            |        |
|  +-------------------------------------------------------------+    |
|  | SHARED INFRASTRUCTURE                                       |    |
|  | Azure Key Vault (all secrets via Managed Identity)          |    | 
|  | Azure App Insights (logs, traces, metrics -- all services)  |    | 
|  | Azure Container Apps (hosts all 3 backend services)         |    |
|  | GitHub Actions CI/CD (build, test, push, deploy)            |    |
|  +-------------------------------------------------------------+    | 
+=====================================================================+
```

---

## 4. Service Descriptions

### 4.1 React Dashboard

```
Type        : Frontend SPA (Single Page Application)
Technology  : React 18, TypeScript, Tailwind CSS
Hosting     : Azure Static Web Apps (free tier)
Talks to    : Azure APIM (all API calls go through gateway)
Auth        : Redirects to Keycloak for login, receives JWT

Responsibilities:
  - Login via Keycloak (redirect to Keycloak login page)
  - Display device list with status per user scope
  - Real-time telemetry charts (last 24 hours)
  - Alert log with AI-generated summaries
  - Cross-mill comparison reports (Customer Admin / Executive)
  - User management UI (Customer Admin only)
```

### 4.2 Keycloak Identity Provider

```
Type        : Identity Provider (IdP)
Technology  : Keycloak 24, PostgreSQL 16
Hosting     : Azure Container Apps
Protocol    : OpenID Connect (OIDC), OAuth 2.0, LDAP

Responsibilities:
  - Issue JWT access tokens and refresh tokens
  - Authenticate users via Azure AD (OIDC federation)
  - Authenticate users via on-premise AD (LDAP federation)
  - Authenticate local users (username/password)
  - Enforce MFA where configured
  - Token expiry: access 8h, refresh 7 days

JWT Token Payload:
  {
    "sub"        : "user-uuid",
    "email"      : "manager@nordpulp.com",
    "tenantId"   : "tenant_nordpulp",
    "role"       : "MillManager",
    "millIds"    : ["mill_lakewood"],
    "areaIds"    : [],
    "exp"        : 1234567890
  }
```

### 4.3 Azure API Management (APIM)

```
Type        : API Gateway
Technology  : Azure API Management (consumption tier)
Hosting     : Azure managed service

Responsibilities:
  - Single HTTPS entry point for all clients
  - Validate JWT token on every request
  - Extract tenantId and role from JWT
  - Forward tenantId as header to downstream services
  - Rate limiting: 1000 req/min per tenant
  - Request/response logging to App Insights
  - Route /api/devices/* --> Device API
  - Route /api/telemetry/* --> Telemetry Service
  - Route /api/alerts/* --> Device API
  - Route /api/reports/* --> Device API

Security Policy (applied to all APIs):
  1. Validate JWT signature against Keycloak JWKS endpoint
  2. Check token expiry
  3. Check required claims (tenantId, role)
  4. Inject X-Tenant-Id header into downstream request
  5. Reject with 401 if any check fails
```

### 4.4 Device API

```
Type        : Backend REST API
Technology  : .NET 9, ASP.NET Core, Clean Architecture
Hosting     : Azure Container Apps
Database    : Azure SQL via EF Core 9
Pattern     : CQRS with MediatR, Repository pattern

Architecture Layers:
  EdgePulse.API/
    Domain/         -- Entities, value objects, domain events
    Application/    -- Commands, queries, handlers, interfaces
    Infrastructure/ -- EF Core, Azure SDK, email service
    WebAPI/         -- Controllers, middleware, DI setup

Responsibilities:
  - Device CRUD (register, update, decommission)
  - Mill and Area management
  - Alert configuration (thresholds per device per metric)
  - Alert lifecycle management (acknowledge, assign, resolve)
  - User invitation and role assignment
  - Audit log recording
  - Reports and analytics queries
  - Enforce tenant isolation on every query
```

### 4.5 Telemetry Service

```
Type        : High-throughput ingestion API
Technology  : Node.js 20, NestJS, TypeScript
Hosting     : Azure Container Apps
Pattern     : Publisher (no database -- stateless)

Responsibilities:
  - Accept POST /telemetry with device API key in header
  - Validate API key against Device API (cached, 5 min TTL)
  - Validate telemetry payload schema
  - Enrich message with tenantId, millId, areaId
  - Publish enriched message to Azure Service Bus queue
  - Return 202 Accepted immediately (fire and forget)
  - Handle 1000+ messages/minute per tenant

Why Node.js here:
  High-concurrency I/O is Node.js's strength.
  Thousands of devices sending readings simultaneously
  is exactly the use case the event loop handles best.
  No CPU-bound work happens here -- just receive, validate,
  publish. Pure I/O.
```

### 4.6 Processor Service

```
Type        : Background worker
Technology  : .NET 9 Worker Service
Hosting     : Azure Container Apps
Databases   : Cosmos DB (write telemetry),
              Azure SQL (write alerts, read thresholds)

Responsibilities:
  - Consume messages from Service Bus queue
  - Deserialize and validate telemetry message
  - Store raw telemetry in Cosmos DB
  - Load threshold config for device + metric
  - Check: has metric breached threshold 3 times in a row?
  - If YES:
      Call Azure OpenAI to generate alert summary
      Create alert record in Azure SQL
      Trigger email notification via SendGrid
      Create in-app notification record
  - If NO:
      Store telemetry only, no alert
  - Dead letter queue: failed messages after 3 retries
```

---

## 5. Communication Patterns

### 5.1 Synchronous (HTTP/REST)

Used for: user interactions, device management, queries, reports.

```
Client --> APIM --> Device API --> Azure SQL
  |
  +--> APIM --> Telemetry Service --> Service Bus

Pattern: Request / Response
Timeout: 30 seconds max
Retry:   3 attempts with exponential backoff
```

### 5.2 Asynchronous (Message Queue)

Used for: telemetry ingestion pipeline only.

```
Telemetry Service --> Service Bus Queue --> Processor Service

Pattern  : Publish / Subscribe (point to point queue)
Guarantee: At-least-once delivery
Order    : FIFO per device session
Retention: 7 days (messages not consumed in 7 days expire)
Dead letter: After 3 failed processing attempts
```

### 5.3 Why This Split

```
Synchronous for management operations:
  Device registration happens once.
  User invitations happen occasionally.
  Alert acknowledgement is user-driven.
  These are low-volume, need immediate response.

Asynchronous for telemetry:
  1000+ messages/minute.
  Processor may be slow or temporarily down.
  Messages cannot be lost.
  Service Bus buffers everything safely.
  Processor catches up when it recovers.
```

### 5.4 Service-to-Service Communication

```
+------------------+    HTTP (internal)    +------------------+
| Telemetry Svc    | --------------------> | Device API       |
| (NestJS)         | GET /internal/devices | (.NET 9)         |
|                  | /validate-key/{key}   |                  |
+------------------+                       +------------------+

Telemetry Service calls Device API to validate device API keys.
Result is cached in memory for 5 minutes to avoid hammering
Device API on every telemetry message.

All internal HTTP calls:
  - Use Managed Identity (no credentials)
  - Go through internal Azure Container Apps network
  - NOT exposed through APIM (internal only)
  - Use circuit breaker pattern (Polly in .NET)
```

---

## 6. Authentication & Authorization Flow

### 6.1 User Login Flow (Azure AD SSO)

```
Step 1: User opens EdgePulse dashboard
        Browser --> React App (Azure Static Web Apps)

Step 2: React detects no JWT token
        React --> redirects to Keycloak login page

Step 3: User clicks "Login with Company Account"
        Keycloak --> redirects to Azure AD login page

Step 4: User enters Stora Enso (NordPulp) AD credentials
        Azure AD authenticates user

Step 5: Azure AD --> returns authorization code to Keycloak

Step 6: Keycloak exchanges code for Azure AD user profile
        Maps Azure AD groups to EdgePulse roles
        Example: "NordPulp-MillManagers-Lakewood" --> MillManager

Step 7: Keycloak issues EdgePulse JWT token
        JWT contains: userId, tenantId, role, millIds, areaIds

Step 8: Keycloak --> redirects back to React with JWT

Step 9: React stores JWT in memory (NOT localStorage)
        React --> all subsequent API calls include JWT in header
        Authorization: Bearer <jwt_token>
```

### 6.2 On-premise AD (LDAP) Flow

```
Same as above except Step 3-5:

Step 3: User clicks "Login with Mill Account"
        Keycloak --> queries on-premise AD via LDAP

Step 4: Keycloak sends LDAP bind request to AD server
        AD server authenticates credentials internally
        No internet required -- all on company network

Step 5: AD returns user profile and group membership
        Keycloak maps AD groups to EdgePulse roles
```

### 6.3 API Request Authorization Flow

```
Every API request follows this chain:

[1] Client sends request with JWT
    GET /api/devices
    Authorization: Bearer eyJhbGc...

[2] APIM validates JWT
    - Check signature against Keycloak JWKS
    - Check expiry
    - Check tenantId claim exists
    If invalid --> return 401 Unauthorized

[3] APIM injects tenant header
    X-Tenant-Id: tenant_nordpulp
    X-User-Role: MillManager
    X-Mill-Ids: mill_lakewood
    Forwards request to Device API

[4] Device API checks role permission
    MillManager can GET devices? YES
    MillManager can DELETE devices? NO --> return 403 Forbidden

[5] Device API enforces tenant isolation
    SELECT * FROM Devices
    WHERE TenantId = 'tenant_nordpulp'  -- from header
    AND MillId IN ('mill_lakewood')      -- from JWT claims

[6] Return filtered response to client
```

### 6.4 Device Telemetry Authentication

```
Devices do NOT use JWT. They use API keys.

[1] Device sends telemetry
    POST /api/telemetry/ingest
    X-Device-Api-Key: dev_key_abc123xyz
    Content-Type: application/json

[2] Telemetry Service validates API key
    Check cache first (5 min TTL)
    If not cached: call Device API GET /internal/devices/key/{key}
    Device API returns: deviceId, tenantId, millId, areaId, status
    If key invalid --> return 401 Unauthorized
    If device status = Decommissioned --> return 403 Forbidden

[3] Telemetry Service enriches message
    Adds tenantId, millId, areaId from key lookup
    Publishes to Service Bus

[4] Processor trusts enriched message
    tenantId already validated upstream
```

---

## 7. Telemetry Pipeline

End-to-end flow from device to dashboard:

```
+----------+     +------------------+     +------------------+
|  Device  |     | Telemetry Svc    |     | Azure Service    |
| PUMP-LW  |     | (NestJS)         |     | Bus Queue        |
+----+-----+     +--------+---------+     +--------+---------+
     |                    |                        |
     | POST /telemetry    |                        |
     | X-Device-Api-Key   |                        |
     | {                  |                        |
     |  deviceId,         |                        |
     |  readings: [       |                        |
     |   {metric,         |                        |
     |    value,unit}     |                        |
     |  ],                |                        |
     |  timestamp         |                        |
     | }                  |                        |
     +------------------->|                        |
                          | validate API key       |
                          | validate schema        |
                          | enrich with tenantId   |
                          | millId, areaId         |
                          |                        |
                          | publish message        |
                          +----------------------->|
                          |                        |
     <-------------------+|                        |
     202 Accepted         |                        |
     (fire and forget)    |                        |
                                                   |
                          +------------------+     |
                          | Processor Svc    |     |
                          | (.NET 9 Worker)  |     |
                          +--------+---------+     |
                                   |               |
                                   | consume       |
                                   |<---------------+
                                   |
                          deserialize message
                                   |
                          store telemetry
                                   |
                                   v
                          +------------------+
                          |   Cosmos DB      |
                          | partition: deviceId
                          +------------------+
                                   |
                          load threshold config
                          for device + metric
                                   |
                          check: 3 consecutive
                          threshold breaches?
                                   |
                   +---------------+---------------+
                   | NO                            | YES
                   v                               v
              done, continue              +------------------+
              next message                | Azure OpenAI     |
                                          | generate summary |
                                          +--------+---------+
                                                   |
                                          +------------------+
                                          | Create Alert     |
                                          | in Azure SQL     |
                                          +--------+---------+
                                                   |
                                          +------------------+
                                          | Notify users     |
                                          | (email + in-app) |
                                          +------------------+
```

### 7.1 Telemetry Message Schema

```json
{
  "messageId"  : "uuid-v4",
  "deviceId"   : "PUMP-LW-001",
  "tenantId"   : "tenant_nordpulp",
  "millId"     : "mill_lakewood",
  "areaId"     : "area_lw_pm1",
  "timestamp"  : "2026-05-13T14:30:45Z",
  "readings"   : [
    { "metric": "temperature",    "value": 92.5, "unit": "C"     },
    { "metric": "inlet_pressure", "value": 3.2,  "unit": "bar"   },
    { "metric": "flow_rate",      "value": 118.0,"unit": "L/min" },
    { "metric": "vibration",      "value": 1.2,  "unit": "mm/s"  },
    { "metric": "power",          "value": 15.8, "unit": "kW"    }
  ]
}
```

### 7.2 Alert Message Schema

```json
{
  "alertId"    : "uuid-v4",
  "deviceId"   : "PUMP-LW-001",
  "tenantId"   : "tenant_nordpulp",
  "millId"     : "mill_lakewood",
  "areaId"     : "area_lw_pm1",
  "metric"     : "temperature",
  "value"      : 92.5,
  "threshold"  : 80.0,
  "unit"       : "C",
  "severity"   : "Critical",
  "status"     : "Open",
  "timestamp"  : "2026-05-13T14:30:45Z",
  "aiSummary"  : "PUMP-LW-001 temperature has exceeded the safe
                  operating threshold of 80C for 3 consecutive
                  readings. Current reading: 92.5C. Immediate
                  inspection recommended.",
  "readings"   : [
    { "timestamp": "2026-05-13T14:30:15Z", "value": 85.1 },
    { "timestamp": "2026-05-13T14:30:30Z", "value": 88.7 },
    { "timestamp": "2026-05-13T14:30:45Z", "value": 92.5 }
  ]
}
```

---

## 8. Data Architecture

### 8.1 Database Responsibility Split

```
+------------------+------------------------------------------+
| Database         | What it stores                           |
+------------------+------------------------------------------+
| Azure SQL        | Tenants, Mills, Areas, Devices           |
|                  | Users, Roles, Area assignments           |
|                  | Alert thresholds (config)                |
|                  | Alerts (lifecycle, AI summary)           |
|                  | Notifications                            |
|                  | Audit logs                               |
+------------------+------------------------------------------+
| Cosmos DB        | Telemetry readings (time-series)         |
|                  | Partition key: deviceId                  |
|                  | High volume, append-only, 12 month TTL   |
+------------------+------------------------------------------+
| PostgreSQL       | Keycloak internal data only              |
|                  | Users, sessions, realms, clients         |
|                  | Managed entirely by Keycloak             |
+------------------+------------------------------------------+
```

### 8.2 Why Two Databases

```
Azure SQL for structured relational data:
  Devices have fixed schema.
  Alerts have relationships (device, user, area).
  Audit logs need strong consistency.
  Reports need SQL JOINs across tables.
  --> Relational database is the right tool.

Cosmos DB for telemetry:
  1000+ readings/minute per tenant.
  Schema varies per device type.
  Partition by deviceId -- all readings for one device
  are on the same partition for fast time-range queries.
  12-month TTL -- automatic expiry, no manual cleanup.
  --> Document database built for this scale.
```

### 8.3 Tenant Isolation Strategy

```
ALL queries in Device API automatically include TenantId filter.
This is enforced at the EF Core DbContext level using
Global Query Filters -- not left to individual developers.

Example EF Core Global Query Filter:
  modelBuilder.Entity<Device>()
    .HasQueryFilter(d => d.TenantId == _currentTenantId);

This means:
  Even if a developer forgets WHERE TenantId = ...,
  EF Core adds it automatically.
  Cross-tenant data leaks are impossible at ORM level.
```

---

## 9. Deployment Architecture

### 9.1 Azure Resources

```
+=====================================================+
|              AZURE SUBSCRIPTION                     |
|                                                     |
|  Resource Group: rg-edgepulse-prod                  |
|                                                     |
|  +-----------------------------------------------+  |
|  | Azure Container Apps Environment              |  |
|  |                                               |  |
|  |  +---------------+  +-------------------+     |  |
|  |  | Device API    |  | Telemetry Service |     |  |
|  |  | .NET 9        |  | Node.js / NestJS  |     |  |
|  |  | 1-3 replicas  |  | 1-5 replicas      |     |  |
|  |  +---------------+  +-------------------+     |  |
|  |                                               |  |
|  |  +---------------+  +-------------------+     |  |
|  |  | Processor Svc |  | Keycloak          |     |  |
|  |  | .NET 9 Worker |  | + PostgreSQL      |     |  |
|  |  | 1-2 replicas  |  | 1 replica         |     |  |
|  |  +---------------+  +-------------------+     |  |
|  +-----------------------------------------------+  |
|                                                     |
|  +---------------+  +---------------------------+   |
|  | Azure APIM    |  | Azure Static Web Apps     |   |
|  | (API Gateway) |  | React Dashboard           |   |
|  +---------------+  +---------------------------+   | 
|                                                     |
|  +---------------+  +---------------+               |
|  | Azure SQL     |  | Cosmos DB     |               |
|  | (relational)  |  | (telemetry)   |               |
|  +---------------+  +---------------+               |
|                                                     |
|  +---------------+  +---------------+               |
|  | Key Vault     |  | App Insights  |               |
|  | (all secrets) |  | (monitoring)  |               |
|  +---------------+  +---------------+               |
|                                                     |
|  +-----------------------------------------------+  |
|  | Azure Service Bus                             |  |
|  | Namespace: sb-edgepulse-prod                  |  |
|  | Queue: telemetry-ingest                       |  |
|  | Queue: notifications                          |  |
|  +-----------------------------------------------+  |
+=====================================================+
```

### 9.2 CI/CD Pipeline

```
Developer pushes to feature branch
          |
          v
GitHub Actions triggers on push
          |
          v
+---------------------------+
| Build & Test Job          |
| - dotnet build            |
| - dotnet test             |
| - npm install             |
| - npm run test            |
+---------------------------+
          |
          | (on merge to main)
          v
+---------------------------+
| Docker Build Job          |
| - docker build            |
| - docker tag with SHA     |
| - docker push to ghcr.io  |
+---------------------------+
          |
          v
+---------------------------+
| Deploy Job                |
| (self-hosted runner)      |
| - pull image from ghcr.io |
| - az containerapp update  |
| - verify health endpoint  |
+---------------------------+
          |
          v
    Live on Azure
```

### 9.3 Environment Strategy

```
+------------+------------------+----------------------------+
| Environment| Branch           | Purpose                    |
+------------+------------------+----------------------------+
| Local      | any              | Developer machine          |
|            |                  | Docker Compose             |
+------------+------------------+----------------------------+
| Production | main             | Live Azure deployment      |
|            |                  | Real Azure services        |
+------------+------------------+----------------------------+

Note: For this portfolio project we use 2 environments only.
In a real enterprise project there would also be:
  staging (pre-production testing)
  UAT (user acceptance testing)
```

---

## 10. Local Development Architecture

### 10.1 Docker Compose Stack

Running the full stack locally with one command:

```
docker compose up

Starts:
  keycloak      --> http://localhost:8080  (admin/admin)
  postgres      --> localhost:5432         (Keycloak DB)
  sqlserver     --> localhost:1433         (Devices DB)
  device-api    --> http://localhost:5000  (.NET 9)
  telemetry-svc --> http://localhost:3000  (Node.js)
  processor     --> background worker
  dashboard     --> http://localhost:4000  (React)
```

### 10.2 Native Development Mode

For faster iteration without Docker overhead:

```
Terminal 1: Start infrastructure only
  docker compose up keycloak postgres sqlserver

Terminal 2: Run Device API natively
  cd src/backend/EdgePulse.API
  dotnet run

Terminal 3: Run Telemetry Service natively
  cd src/EdgePulse.TelemetryService
  npm run start:dev

Terminal 4: Run Dashboard natively
  cd src/EdgePulse.Dashboard
  npm start
```

### 10.3 Device Simulator

For development and demo purposes, a simulator generates
realistic telemetry for NordPulp Industries mills:

```
cd tools/DeviceSimulator
dotnet run --mill lakewood --devices 10 --interval 5

Simulates:
  10 devices in Lakewood Mill
  Sends readings every 5 seconds
  Randomly triggers threshold breaches
  for demo and testing purposes
```

---

## 11. On-Premise Deployment with HAProxy

Some industrial mills have no internet connectivity.
They cannot use Azure Container Apps or any cloud services.
EdgePulse supports a fully on-premise deployment mode
using HAProxy as the load balancer in front of Docker containers.

### 11.1 What is HAProxy

HAProxy (High Availability Proxy) is a battle-tested open source
load balancer used in industrial and enterprise environments worldwide.
It routes traffic to healthy containers and tracks each backend server
in one of three states:

```
ACTIVE   -> Container is healthy, receiving traffic normally.
            Health check passing every 5 seconds.
            Gets full share of incoming requests.

DRAIN    -> Container is being gracefully shut down.
            No NEW connections routed to it.
            Existing in-flight requests complete naturally.
            Used during rolling deployments for zero downtime.

INACTIVE -> Container is unhealthy or stopped.
            Health check failing (3 consecutive failures).
            Zero traffic sent.
            HAProxy monitors and restores when healthy again.
```

### 11.2 On-Premise vs Cloud Deployment

```
+====================+==========================+==========================+
|                    |  CLOUD (Azure)           |  ON-PREMISE (Mill)       |
+====================+==========================+==========================+
| Load Balancer      | Azure Container Apps     | HAProxy                  |
| Health Checks      | Azure built-in           | HAProxy /health endpoint |
| Auto Scaling       | Yes (CPU/queue trigger)  | No (fixed replicas)      |
| Zero-downtime      | Rolling update via Azure | HAProxy drain state      |
| Internet required  | Yes                      | No                       |
| Secrets            | Azure Key Vault          | HashiCorp Vault or files |
| Monitoring         | Azure App Insights       | HAProxy stats + logs     |
| Same Docker image  | Yes                      | Yes (identical image)    |
+====================+==========================+==========================+

Key point: Identical Docker images are used in both modes.
No code changes between cloud and on-premise deployment.
Only infrastructure configuration differs.
```

### 11.3 On-Premise Architecture Diagram

```
+==============================================================+
|              MILL ON-PREMISE DEPLOYMENT                      |
|              (No internet required)                          |
|                                                              |
|  +-------------------+    +----------------------------+     |
|  | HAProxy           |    | Keycloak + PostgreSQL      |     |
|  | :80 / :443        |    | + On-premise AD (LDAP)     |     |
|  | :8404 (stats UI)  |    | No Azure AD -- LDAP only   |     |
|  +--------+----------+    +----------------------------+     |
|           |                                                  |
|    Health check /health every 5s                             |
|    Active / Drain / Inactive state per container             |
|           |                                                  |
|    +------+-------+      +-----------+                       |
|    | /api/*       |      | /telemetry|                       |
|    v              v      v           v                       |
|  +----------+ +----------+ +----------+ +----------+         |
|  | device   | | device   | | telemetry| | telemetry|         |
|  | api-1    | | api-2    | | svc-1    | | svc-2    |         |
|  | (active) | | (active) | | (active) | | (drain)  |         |
|  +----------+ +----------+ +----------+ +----------+         |
|                                                              |
|  +-------------------+    +----------------------------+     |
|  | Processor Svc     |    | RabbitMQ                   |     |
|  | (.NET 9 Worker)   |    | (replaces Azure Service    |     |
|  | 1-2 instances     |    |  Bus for on-premise)       |     |
|  +-------------------+    +----------------------------+     |
|                                                              |
|  +-------------------+    +----------------------------+     |
|  | SQL Server        |    | MongoDB                    |     |
|  | (devices, alerts) |    | (telemetry -- replaces     |     |
|  +-------------------+    |  Cosmos DB on-premise)     |     |
|                            +----------------------------+    |
|                                                              |
|  +----------------------------------------------------------+|
|  | HAProxy Stats Dashboard: http://mill-server:8404/stats   ||
|  | Shows: active/drain/inactive per backend, req/s, errors  ||
|  +----------------------------------------------------------+|
+==============================================================+
```

### 11.4 HAProxy Configuration

```
#------------------------------------------------------
# EdgePulse HAProxy Configuration
# On-premise mill deployment
#------------------------------------------------------

global
    log stdout format raw local0
    maxconn 50000

defaults
    mode    http
    timeout connect  5s
    timeout client  30s
    timeout server  30s
    option  httplog
    option  forwardfor
    option  http-server-close
    option  redispatch
    retries 3

#------------------------------------------------------
# HAProxy Stats Dashboard
# Access: http://mill-server:8404/stats
# Shows container states: active / drain / inactive
#------------------------------------------------------
frontend stats
    bind *:8404
    stats enable
    stats uri /stats
    stats refresh 5s
    stats show-legends
    stats show-node
    stats auth admin:edgepulse123

#------------------------------------------------------
# Main Entry Point
#------------------------------------------------------
frontend edgepulse_frontend
    bind *:80
    bind *:443 ssl crt /certs/edgepulse.pem

    # Route telemetry ingestion to telemetry pool
    acl is_telemetry  path_beg /api/telemetry
    acl is_telemetry  path_beg /api/ingest

    # Route identity to Keycloak
    acl is_auth       path_beg /auth

    use_backend telemetry_pool if is_telemetry
    use_backend keycloak_pool  if is_auth
    default_backend deviceapi_pool

#------------------------------------------------------
# Device API Backend Pool
# Load balance: round robin (equal distribution)
# Health check: GET /health every 5s
# Fall: 3 consecutive failures -> inactive
# Rise: 2 consecutive passes  -> active again
#------------------------------------------------------
backend deviceapi_pool
    balance roundrobin
    option  httpchk GET /health
    http-check expect status 200

    default-server inter 5s fall 3 rise 2

    server device-api-1 device-api-1:5000 check
    server device-api-2 device-api-2:5000 check
    # backup: only used if all primary servers are inactive
    server device-api-3 device-api-3:5000 check backup

#------------------------------------------------------
# Telemetry Service Backend Pool
# Load balance: least connections
# (send to server with fewest active connections)
# Better for high-volume unequal workloads
#------------------------------------------------------
backend telemetry_pool
    balance leastconn
    option  httpchk GET /health
    http-check expect status 200

    default-server inter 5s fall 3 rise 2

    server telemetry-1 telemetry-1:3000 check
    server telemetry-2 telemetry-2:3000 check

#------------------------------------------------------
# Keycloak Backend Pool
# Single instance (stateful -- session data in PostgreSQL)
#------------------------------------------------------
backend keycloak_pool
    balance roundrobin
    option  httpchk GET /auth/health/ready
    http-check expect status 200

    default-server inter 10s fall 3 rise 2

    server keycloak-1 keycloak:8080 check
```

### 11.5 Zero-Downtime Deployment with Drain

When deploying a new version on-premise:

```
BEFORE DEPLOYMENT:
  device-api-1  (active)  <- serving traffic
  device-api-2  (active)  <- serving traffic
  device-api-3  (backup)  <- standby

STEP 1: Start new container with updated image
  device-api-4 starts
  HAProxy health check passes after 2 checks (10s)
  device-api-4 (active) <- now serving traffic

STEP 2: Drain device-api-1
  HAProxy marks device-api-1 as DRAIN
  No new connections sent to device-api-1
  In-flight requests complete naturally (up to 30s)

  device-api-1  (drain)  <- finishing existing requests
  device-api-2  (active) <- serving new requests
  device-api-4  (active) <- serving new requests

STEP 3: After drain timeout (30s)
  device-api-1 stops cleanly
  Zero requests dropped

STEP 4: Repeat for device-api-2
  Rolling deployment complete
  Zero downtime achieved
```

### 11.6 Docker Compose -- On-Premise Mode

```yaml
# docker-compose.onpremise.yml
# Run with: docker compose -f docker-compose.onpremise.yml up

services:

  haproxy:
    image: haproxy:2.8-alpine
    ports:
      - "80:80"
      - "443:443"
      - "8404:8404"
    volumes:
      - ./infrastructure/haproxy/haproxy.cfg:/usr/local/etc/haproxy/haproxy.cfg:ro
      - ./infrastructure/certs:/certs:ro
    depends_on:
      device-api-1:
        condition: service_healthy
      telemetry-1:
        condition: service_healthy

  device-api-1:
    image: ghcr.io/rakshins10/edgepulse-api:latest
    environment:
      - INSTANCE_ID=device-api-1
      - DEPLOYMENT_MODE=onpremise
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
      interval: 10s
      timeout: 5s
      retries: 3

  device-api-2:
    image: ghcr.io/rakshins10/edgepulse-api:latest
    environment:
      - INSTANCE_ID=device-api-2
      - DEPLOYMENT_MODE=onpremise
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
      interval: 10s
      timeout: 5s
      retries: 3

  telemetry-1:
    image: ghcr.io/rakshins10/edgepulse-telemetry:latest
    environment:
      - INSTANCE_ID=telemetry-1
      - DEPLOYMENT_MODE=onpremise
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:3000/health"]
      interval: 10s
      timeout: 5s
      retries: 3

  telemetry-2:
    image: ghcr.io/rakshins10/edgepulse-telemetry:latest
    environment:
      - INSTANCE_ID=telemetry-2
      - DEPLOYMENT_MODE=onpremise
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:3000/health"]
      interval: 10s
      timeout: 5s
      retries: 3

  processor:
    image: ghcr.io/rakshins10/edgepulse-processor:latest
    environment:
      - DEPLOYMENT_MODE=onpremise

  rabbitmq:
    image: rabbitmq:3.12-management-alpine
    ports:
      - "15672:15672"   # RabbitMQ management UI
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=EdgePulse@2026
    volumes:
      - sqlserver_data:/var/opt/mssql

  mongodb:
    image: mongo:7.0
    volumes:
      - mongodb_data:/data/db

  keycloak:
    image: quay.io/keycloak/keycloak:24.0
    command: start-dev
    environment:
      - KC_DB=postgres
    volumes:
      - keycloak_data:/opt/keycloak/data

  postgres:
    image: postgres:16-alpine
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  rabbitmq_data:
  sqlserver_data:
  mongodb_data:
  keycloak_data:
  postgres_data:
```

### 11.7 Service Replacement for On-Premise

On-premise mode replaces Azure-specific services with open source equivalents:

```
+------------------------+------------------+---------------------------+
| Cloud (Azure)          | On-Premise       | Why                       |
+------------------------+------------------+---------------------------+
| Azure Service Bus      | RabbitMQ         | Open source message queue |
| Azure Cosmos DB        | MongoDB          | Document DB, same API     |
| Azure Key Vault        | HashiCorp Vault  | Secret management         |
| Azure App Insights     | Grafana + Loki   | Logs and metrics          |
| Azure Container Apps   | Docker + HAProxy | Container orchestration   |
| Azure Static Web Apps  | Nginx            | Static file serving       |
+------------------------+------------------+---------------------------+

Application code does NOT change between modes.
Configuration switches via DEPLOYMENT_MODE environment variable.
Service abstractions in code hide the difference:
  IMessageQueue -> AzureServiceBusQueue (cloud)
               -> RabbitMqQueue        (on-premise)
  ITelemetryStore -> CosmosDbStore     (cloud)
                  -> MongoDbStore      (on-premise)
```

---

## 12. Architecture Decision Records (ADRs)

ADRs document WHY we made key decisions.
This is what senior engineers and architects produce.

### ADR-001: Microservices over Monolith

```
Date    : May 2026
Status  : Accepted

Context:
  EdgePulse has two very different workloads:
  1. Device management (low volume, complex business logic)
  2. Telemetry ingestion (high volume, simple logic)

Decision:
  Use microservices -- separate Device API and Telemetry Service.

Reasons:
  - Telemetry Service can scale independently (more replicas)
    without scaling the Device API unnecessarily.
  - Different tech stacks match different strengths:
    Node.js for I/O-heavy telemetry ingestion.
    .NET for complex business logic in Device API.
  - Failure isolation: if Device API is down,
    telemetry ingestion still works via Service Bus.

Trade-offs:
  - More complex local development setup.
  - Network calls between services add latency.
  - Mitigated by: Docker Compose for local dev,
    internal Container Apps network for production.
```

### ADR-002: Azure Service Bus over Direct HTTP for Telemetry

```
Date    : May 2026
Status  : Accepted

Context:
  Telemetry arrives at 1000+ messages/minute.
  Processor needs to check thresholds and call OpenAI.
  OpenAI calls are slow (1-2 seconds each).

Decision:
  Use Azure Service Bus queue between ingestion and processing.

Reasons:
  - Decouples ingestion speed from processing speed.
  - If Processor is slow or down, messages queue up safely.
  - Telemetry Service returns 202 immediately -- device
    does not wait for processing to complete.
  - Service Bus retries failed messages automatically.
  - Dead letter queue captures persistently failing messages.

Trade-offs:
  - Alert is not immediate -- delay of seconds to minutes
    depending on queue depth.
  - Acceptable because alert threshold is 3 consecutive
    readings -- not a real-time emergency system.
```

### ADR-003: API Gateway (APIM) over Direct Service Access

```
Date    : May 2026
Status  : Accepted

Context:
  React dashboard needs to call multiple backend services.
  JWT validation needs to happen consistently.

Decision:
  Use Azure API Management as single entry point.

Reasons:
  - Single URL for all clients (no CORS complexity).
  - JWT validation in one place -- services trust APIM.
  - Rate limiting enforced centrally.
  - Easy to add new services without changing client code.
  - Built-in request/response logging.

Trade-offs:
  - Additional cost (consumption tier: pay per call).
  - Additional latency (~10ms per request).
  - For local dev: use YARP reverse proxy instead of APIM.
```

### ADR-004: Keycloak over ASP.NET Core Identity

```
Date    : May 2026
Status  : Accepted

Context:
  NordPulp Industries uses Azure AD for employee accounts.
  Some mills use on-premise AD with no internet.
  Need SSO, MFA, and RBAC.

Decision:
  Use Keycloak as identity broker.

Reasons:
  - Supports both Azure AD (OIDC) and on-premise AD (LDAP).
  - No internet required for LDAP federation.
  - Enterprise-grade SSO out of the box.
  - ASP.NET Core Identity would require building all of
    this from scratch.
  - Keycloak is what ABB, Siemens, Bosch actually use.

Trade-offs:
  - Keycloak requires PostgreSQL -- one more database.
  - Higher memory footprint than a simple JWT library.
  - Mitigated by: PostgreSQL is free, Keycloak runs in
    a single Container App replica.
```

### ADR-005: Cosmos DB for Telemetry over SQL

```
Date    : May 2026
Status  : Accepted

Context:
  1000+ telemetry readings/minute per tenant.
  12 months retention.
  Different device types have different metrics.
  Query pattern: all readings for device X in time range Y.

Decision:
  Use Azure Cosmos DB for telemetry storage.

Reasons:
  - Partition key = deviceId means all readings for one
    device are co-located -- extremely fast time-range queries.
  - Schema-flexible -- pump metrics differ from motor metrics.
  - TTL (time-to-live) on documents handles 12-month
    retention automatically.
  - Horizontal scaling handles volume growth.
  - SQL Server would require complex partitioning for this
    volume and struggle with schema flexibility.

Trade-offs:
  - No SQL JOINs -- cannot JOIN telemetry with device info.
  - Mitigated by: denormalizing deviceName, millId into
    each telemetry document at write time.
  - Higher cost than SQL at high volume.
  - Mitigated by: free tier covers dev/demo usage.
```

### ADR-006: HAProxy for On-Premise Deployment

```
Date   : May 2026
Status : Accepted

Context:
  Some industrial mills (like paper mills) have no internet.
  They cannot use Azure Container Apps or any Azure services.
  They still need load balancing, health-aware routing,
  and zero-downtime deployments for EdgePulse.

Decision:
  Support on-premise deployment mode using HAProxy
  as load balancer in front of Docker containers.
  Replace Azure-specific services with open source equivalents:
  RabbitMQ, MongoDB, HashiCorp Vault, Grafana.

Reasons:
  - HAProxy is battle-tested in industrial environments.
  - Active/Drain/Inactive states enable zero-downtime
    rolling deployments inside the mill network.
  - HAProxy stats dashboard gives ops visibility without cloud.
  - Same Docker images work in both cloud and on-premise.
  - No code changes between deployment modes.
  - Directly relevant to ABB, Siemens, Bosch deployments
    where on-premise is the standard.
  - Strong interview talking point for industrial companies.

Trade-offs:
  - No auto-scaling on-premise (fixed replicas).
  - Ops team manages HAProxy config manually.
  - More complex infrastructure config.
  - Mitigated by: well-documented config, clear runbook,
    only 2 replicas needed for typical mill workload.
```

---

## 13. Non-Functional Architecture

### 12.1 Scalability

```
Horizontal scaling via Container Apps replicas:

Service            Min    Max    Scale trigger
-----------        ---    ---    -------------
Device API          1      3     CPU > 70%
Telemetry Svc       1      5     CPU > 60% / queue depth
Processor           1      2     Service Bus queue depth
Keycloak            1      1     Manual (stateful)
```

### 12.2 Reliability

```
Service Bus: at-least-once delivery guarantee
             messages survive Processor restart
             dead letter queue for poison messages

Azure SQL:   geo-redundant backups (automated)
             point-in-time restore up to 35 days

Cosmos DB:   automatic replication
             99.999% availability SLA

Container Apps: automatic restart on crash
                health probes on all services
```

### 12.3 Observability

```
Every service sends to App Insights:

Structured Logs:
  Every request, response, error logged
  Include: tenantId, userId, correlationId

Distributed Traces:
  One request traced across all services
  Correlate: APIM -> Device API -> SQL

Custom Metrics:
  telemetry_messages_per_minute (per tenant)
  alert_rate (anomalies / total readings)
  api_response_time_p95

Alerts:
  Alert if error rate > 1% in 5 minutes
  Alert if telemetry queue depth > 10,000
  Alert if any service is down > 1 minute
```

---

## 14. Security Architecture

```
LAYER 1 -- Transport
  All traffic over HTTPS / TLS 1.2+
  HTTP redirects to HTTPS automatically

LAYER 2 -- Authentication
  Users: JWT from Keycloak (8h expiry)
  Devices: API key per device
  Service-to-service: Managed Identity

LAYER 3 -- Authorization
  APIM validates JWT on every request
  Device API checks role permissions
  EF Core global filters enforce tenant isolation

LAYER 4 -- Secrets
  Zero hardcoded secrets in code or config
  All secrets in Azure Key Vault
  Services access Key Vault via Managed Identity
  No service principal keys stored anywhere

LAYER 5 -- Network
  Container Apps internal network for service-to-service
  Only APIM and Static Web Apps exposed to internet
  Service Bus accessible only from Container Apps network

LAYER 6 -- Audit
  All user actions logged immutably
  Log retention: 24 months
  Tamper-proof: no DELETE on audit table
```

---

*Document ends. Next: 03-data-design.md*