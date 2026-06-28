# EdgePulse — Project Guide

> Engineering standards, architecture rules, and project conventions for EdgePulse.
> Keep it updated. It is the single source of truth for project context.
> Last updated: May 2026 | Sprint 11 complete — Sprint 12 next

---

## What Is EdgePulse?

EdgePulse is a production-grade, multi-tenant Industrial IoT Device Management Platform.
It is being built as both a sellable SaaS product AND a senior engineering portfolio piece.

The core value proposition: enterprise-grade industrial monitoring at 10x lower cost,
with on-premise support, full configurability, and industry-specific templates —
targeting mid-market manufacturers (50-500 employees) that ABB/Siemens/Honeywell ignore.

**Developer:** Rakshith N S — 10+ years .NET, R&D Specialist at ABB Finland (Pulp & Paper MES)
**GitHub:** https://github.com/rakshins10/EdgePulse
**Local:** C:\Studies\EdgePulse-Application\EdgePulse
**Board:** https://github.com/users/rakshins10/projects (EdgePulse Development)
**Swagger:** http://localhost:5104/swagger (when running locally)

---

## Tech Stack

```
Backend API:      .NET 9, ASP.NET Core, Clean Architecture
                  CQRS + MediatR 12, EF Core 9.0.5 (PINNED), FluentValidation
Ingestion:        Node.js 20, NestJS, TypeScript    [Sprint 5 - BUILT]
Processor:        .NET 9 Worker Service             [Sprint 6 - BUILT]
OPC-UA Agent:     Node.js 20, TypeScript, node-opcua [Sprint 11 - BUILT]
Identity:         Keycloak 24 + PostgreSQL 16        [Sprint 4 - BUILT]
Message Queue:    Azure Service Bus (cloud) / RabbitMQ (on-premise)
Telemetry DB:     Azure Cosmos DB (cloud) / MongoDB 7 (on-premise)
Primary DB:       Azure SQL (cloud) / SQL Server 2022 (on-premise, Docker)
Load Balancer:    HAProxy (on-premise) / Azure Container Apps (cloud)
AI:               Azure OpenAI GPT-4o-mini (cloud) / Ollama llama3.2 (on-premise)
Frontend:         React 18 + TypeScript + Vite + CSS Modules [Sprint 7 - BUILT]
CI/CD:            GitHub Actions + self-hosted runner [NOT YET BUILT]
```

**CRITICAL PACKAGE PINS — DO NOT UPGRADE:**
- EF Core: `9.0.5` (9.0.6+ breaks migrations)
- Swashbuckle: `6.9.0` (7.x breaks .NET 9 Swagger)
- JWT Bearer: `9.0.5`

**FRONTEND RULE — NO CSS LIBRARIES:**
Plain CSS Modules only. No Tailwind, no Bootstrap, no Radix UI, no component libraries.
Theme: CSS custom properties in `index.css` (`:root` dark, `[data-theme="light"]` light).

---

## Solution Structure

```
src/
├── EdgePulse.sln
├── EdgePulse.Domain/           # No dependencies. Pure C#.
│   ├── Common/
│   │   ├── BaseEntity.cs       # Id, CreatedAt, UpdatedAt, IsDeleted, DeletedAt
│   │   ├── TenantBaseEntity.cs # + TenantId
│   │   └── LookupBaseEntity.cs # + Code, IsSystem, IsActive, SortOrder, TemplateId
│   ├── Entities/               # Tenant, Mill, Area, Device, Alert, AlertThreshold, ...
│   ├── Enums/                  # UserRole, DeploymentMode, RoleScope
│   └── Constants/
│       ├── WellKnownIds.cs     # System GUIDs (00000010-..., 00000011-..., etc.)
│       ├── DemoIds.cs          # Fixed NordPulp demo GUIDs (10000001-..., 40000001-...)
│       ├── LookupTypes.cs      # "DeviceType", "DeviceStatus", etc.
│       ├── EntityTypes.cs
│       └── FileCategories.cs
│
├── EdgePulse.Application/      # Depends on Domain only.
│   ├── Common/Interfaces/
│   │   ├── IApplicationDbContext.cs
│   │   ├── ICurrentUserService.cs
│   │   └── IFileStorageService.cs
│   ├── Common/Exceptions/      # ValidationException(400), NotFoundException(404),
│   │                           # ForbiddenAccessException(403), ConflictException(409)
│   ├── Common/Behaviours/      # ValidationBehaviour, LoggingBehaviour (MediatR pipeline)
│   └── Features/
│       ├── Configuration/      # Device types, statuses, metric types, alert severities,
│       │                       # manufacturers, location types, maintenance types,
│       │                       # industry templates, lookup overrides
│       ├── Tenants/
│       ├── Mills/
│       ├── Areas/
│       ├── Devices/
│       └── Alerts/             # AlertThreshold CRUD + Alert state machine (CQRS)
│
├── EdgePulse.Infrastructure/   # Depends on Application + Domain.
│   └── Persistence/
│       ├── EdgePulseDbContext.cs
│       ├── Configurations/     # IEntityTypeConfiguration<T> per entity
│       ├── Migrations/         # EF Core migrations
│       └── Seeding/
│           └── DemoSeedService.cs  # NordPulp demo seed (idempotent, fixed GUIDs)
│
├── EdgePulse.API/              # Depends on Application + Infrastructure.
│   ├── Controllers/
│   ├── Middleware/ExceptionHandlingMiddleware.cs
│   └── Program.cs              # --seed flag runs DemoSeedService then exits
│
├── EdgePulse.TelemetryProcessor/  # .NET 9 Worker Service
│   ├── Worker.cs               # RabbitMQ consumer → MongoDB + AlertEngine
│   ├── Services/
│   │   ├── AlertEngineService.cs   # Threshold eval, consecutive-breach tracking
│   │   └── ThresholdCacheService.cs # 60s ADO.NET cache of AlertThresholds
│   └── Models/TelemetryReading.cs
│
├── EdgePulse.Ingestion/        # Node.js 20 + NestJS — HTTP → RabbitMQ gateway
│
├── EdgePulse.OpcUaAgent/       # Node.js 20 + TypeScript
│   ├── src/
│   │   ├── opcua/OpcUaSubscriber.ts    # node-opcua client, subscription-based
│   │   ├── publisher/RabbitMqPublisher.ts
│   │   ├── simulator/OpcUaSimulator.ts # Demo OPC-UA server (--simulate flag)
│   │   └── simulator/profiles.ts       # 20 devices, spike profiles per threshold
│   └── config/config.nordpulp.json     # All 20 NordPulp devices pre-wired
│
└── EdgePulse.Dashboard/        # React 18 + TypeScript + Vite
    └── src/
        ├── context/ThemeContext.tsx    # dark/light theme, localStorage persist
        ├── components/layout/          # AppLayout, Sidebar (mobile drawer), ThemeToggle
        ├── pages/alerts/AlertsPage.tsx # Paginated alerts, acknowledge/resolve modal
        ├── store/                      # Redux: alert count for sidebar badge
        └── api/                        # Axios + Keycloak bearer token interceptor
```

---

## Domain Model

### Organisational Hierarchy

```
EdgePulse Platform (SuperAdmin)
  └── Tenant (e.g. NordPulp Industries)
        └── Mill (e.g. Lakewood Mill)
              └── Area (e.g. Fiberline)
                    └── Device (e.g. PUMP-LW-001)
                          └── AlertThreshold  (threshold rule per metric)
                          └── Alert           (fired when threshold breached N times)
```

### Roles (5 total)

| Role | Scope | Can Do |
|------|-------|--------|
| SuperAdmin | Platform | All tenants, all mills, create tenants |
| CustomerAdmin | Tenant | All mills within tenant |
| MillManager | Single Mill | One mill, create areas/devices/thresholds |
| Operator | Assigned Areas | Read + acknowledge alerts |
| Executive | Tenant (read-only) | Dashboard and reports only |

### Key Entities

```
Tenant           -> Name, Slug (unique), ContactEmail, Status
Mill             -> TenantId, Name, Code, Location, Timezone, HasInternet, DeploymentMode
Area             -> TenantId, MillId, Name, Code, LocationTypeId, Description
Device           -> TenantId, MillId, AreaId, TypeId, StatusId, Name, Code, SerialNumber
DeviceApiKey     -> TenantId, DeviceId, KeyHash (SHA-256), KeyPrefix, IsActive
AlertThreshold   -> DeviceId, MetricKey, MinValue?, MaxValue?, SeverityCode, ConsecutiveCount
Alert            -> DeviceId, AlertThresholdId, MetricKey, TriggerValue, StatusCode
                    States: OPEN → ACKNOWLEDGED → RESOLVED → CLOSED
```

### Lookup Architecture (CRITICAL PATTERN)

Every dropdown in the system reads from a configurable lookup table. Never hardcode.

```
System values:  TenantId = null, IsSystem = true  (seeded, protected)
Custom values:  TenantId = <id>, IsSystem = false  (created by tenant)
Overrides:      TenantLookupOverride table (rename or disable system values per tenant)
```

Well-known GUID prefixes:
- `00000010-*` = Industry Templates
- `00000011-*` = Pulp & Paper Device Types
- `00000032-*` = Generic Device Statuses
- `00000033-*` = Generic Alert Severities
- `00000034-*` = Generic Alert Statuses
- `00000035-*` = Generic Metric Types
- `00000099-*` = Dev/test tenant (placeholder)

Demo GUIDs (DemoIds.cs):
- `10000001-*` = NordPulp Tenant
- `20000001-*` = NordPulp Mills
- `30000001-*` = Lakewood Areas / `30000002-*` = Riverside Areas
- `40000001-*` = Lakewood Devices / `40000002-*` = Riverside Devices
- `50000001-*` = Lakewood Thresholds / `50000002-*` = Riverside Thresholds

---

## Coding Standards (NEVER VIOLATE)

### 1. No Hardcoded Values
```csharp
// WRONG
if (status == "ONLINE") { ... }

// CORRECT
if (status == GenericDeviceStatusIds.Online.ToString()) { ... }
```

### 2. Clean Architecture — Dependency Rules
```
Domain       <- no dependencies
Application  <- Domain only
Infrastructure <- Application + Domain
API          <- Application + Infrastructure
```
Never reference Infrastructure from Application. Never reference API from anywhere else.

### 3. CQRS with MediatR
```csharp
public record CreateDeviceTypeCommand(...) : IRequest<Guid>;  // write
public record GetDeviceTypesQuery()        : IRequest<List<DeviceTypeDto>>;  // read
```

### 4. IApplicationDbContext — IQueryable Pattern
```csharp
IQueryable<DeviceType> DeviceTypes { get; }  // interface
// Global Query Filters auto-apply TenantId + IsDeleted — never write manual WHERE
```

### 5. Soft Delete Only
```csharp
entity.Deactivate(); // IsDeleted = true, DeletedAt = UtcNow
_context.Update(entity);
await _context.SaveChangesAsync(ct);
```

### 6. Request Models for POST/PUT (not Commands directly in controller)
```csharp
// CORRECT — separate request record, map to command in controller
public async Task<IActionResult> Create([FromBody] CreateXRequest request, ...)
    => Ok(await _mediator.Send(new CreateXCommand(request.Name, ...)));
```

### 7. All Timestamps UTC
```csharp
CreatedAt = DateTime.UtcNow  // always, never DateTime.Now
```

### 8. Exception Pattern
```csharp
throw new NotFoundException(nameof(Device), id);   // -> 404
throw new ForbiddenAccessException();               // -> 403
throw new ConflictException("Code exists.");        // -> 409
// ValidationException thrown by ValidationBehaviour -> 400
```

### 9. Alert SeverityCode / StatusCode — stored as plain strings
```csharp
// Stored as nvarchar, NOT FK to lookup tables
// Reason: TelemetryProcessor writes alerts without loading EF / joining lookups
// Valid values: CRITICAL / HIGH / MEDIUM / LOW  (severity)
//               OPEN / ACKNOWLEDGED / RESOLVED / CLOSED  (status)
```

### 10. CSS — NO libraries, CSS Modules only
```css
/* All theme colours via CSS custom properties — index.css */
background: var(--color-surface);
color: var(--color-text);
border: 1px solid var(--color-border);
/* No Tailwind, no Bootstrap, no Radix UI */
```

---

## Local Infrastructure

```bash
# Start all services (including OPC-UA simulator + agent)
cd /c/Studies/EdgePulse-Application/EdgePulse
docker compose -f infrastructure/docker-compose.onpremise.yml up -d

# Stop
docker compose -f infrastructure/docker-compose.onpremise.yml down
```

| Service | URL / Host | Credentials |
|---------|-----------|-------------|
| Swagger | http://localhost:5104/swagger | n/a |
| HAProxy Stats | http://localhost:8404/stats | admin/edgepulse123 |
| Keycloak | http://localhost:8080 | admin/admin |
| RabbitMQ UI | http://localhost:15672 | edgepulse/EdgePulse@2026 |
| SQL Server | localhost:1433 | sa/EdgePulse@2026 |
| MongoDB | localhost:27017 | edgepulse/EdgePulse@2026 |
| PostgreSQL | localhost:5432 | keycloak/keycloak |
| OPC-UA Simulator | opc.tcp://localhost:4840 | anonymous |

---

## Demo Data Seed

```bash
# Run once after applying migrations — fully idempotent, safe to re-run
dotnet run --project src/EdgePulse.API --seed
```

Seeds: NordPulp tenant, 2 mills, 8 areas, 20 devices, 21 alert thresholds.
Full reference: `docs/domain/02-demo-data-setup.md`

---

## EF Core Migrations

```bash
cd /c/Studies/EdgePulse-Application/EdgePulse/src

dotnet ef migrations add <Name> \
  --project EdgePulse.Infrastructure \
  --startup-project EdgePulse.API

dotnet ef database update \
  --project EdgePulse.Infrastructure \
  --startup-project EdgePulse.API
```

**IMPORTANT:** If migration has `InsertData` for a record already in DB,
manually remove the `InsertData` block from the migration file before running update.

---

## GitHub Workflow

```bash
export PATH=$PATH:"/c/Program Files/GitHub CLI"
```

### Per-Sprint Workflow
```
1. git checkout -b feature/sprint-N-name
2. Code + commit with issue reference: "feat: description #XX"
3. git push origin feature/sprint-N-name
4. gh pr create --title "..." --body "Closes #XX"
5. Merge PR → issue auto-closes
6. git checkout main && git pull
7. Create docs/sprints/sprint-N-name.md summary
```

### Commit Message Format
```
feat / fix / docs / infra / chore / refactor
Always include issue reference: "feat: description #XX"
```

---

## Current Sprint Status

| Sprint | Topic | Status | Issue |
|--------|-------|--------|-------|
| 1 | Config Module (lookup tables) | ✅ Done | #1 |
| 2 | Organisation Module (tenant/mill/area) | ✅ Done | #2 |
| 3 | Device Management | ✅ Done | #3 |
| 4 | Keycloak JWT Auth | ✅ Done | — |
| 5 | Telemetry Ingestion (NestJS + RabbitMQ) | ✅ Done | — |
| 6 | TelemetryProcessor Worker | ✅ Done | — |
| 7 | React Dashboard (initial) | ✅ Done | — |
| 8 | Alerts Engine (thresholds, state machine, API) | ✅ Done | #64 |
| 9 | Demo Data Seed (NordPulp fixed GUIDs) | ✅ Done | #65 |
| 10 | Dark Mode + Responsive Layout | ✅ Done | #66 |
| 11 | OPC-UA Edge Agent + Simulator | ✅ Done | #67 |
| **12** | **Executive Dashboard** | **🔜 Next** | #68 |

### Next Sprints

```
Sprint 12: Executive Dashboard — KPI tiles, 7-day alert chart (plain SVG), top devices
Sprint 13: CI/CD Pipeline — GitHub Actions, Docker build + push, auto-deploy
Sprint 14: AI Features — GPT-4o-mini alert summaries (Azure OpenAI + Ollama fallback)
Sprint 15: User Management UI — role assignment, AD group mapping
           + Documentation Sprints (#72-#78)
```

---

## Completed API Endpoints

### ConfigurationController (`/api/configuration/`)
```
GET/POST/PUT/DELETE  device-types
GET/POST/PUT/DELETE  device-statuses
GET/POST             metric-types
GET/POST/PUT/DELETE  alert-severities
GET                  alert-statuses
GET                  industry-templates  (SuperAdmin only)
GET/POST             manufacturers
POST                 device-models
GET/POST             maintenance-types
GET/POST             location-types
GET/PUT/DELETE       lookup-overrides
```

### OrganisationController (`/api/organisation/`)
```
GET/POST    tenants   (SuperAdmin)
GET/POST    mills
GET/POST    areas
```

### DevicesController (`/api/devices/`)
```
GET/POST    devices   (POST returns API key once)
```

### AlertsController (`/api/alerts/`)
```
GET/POST/PUT/DELETE   thresholds
GET                   (paginated list, ?millId= ?deviceId= ?severityCode= ?statusCode=)
GET                   count  (returns openCount + criticalOpenCount for sidebar badge)
POST                  {id}/acknowledge
POST                  {id}/resolve
```

---

## Keycloak Configuration

```
Realm:         edgepulse
Client ID:     edgepulse-api
Client Secret: lnBQYXdQnQTku1jT64LbEMyaRFRws3HS  (dev — rotate before prod)
Authority:     http://localhost:8080/realms/edgepulse
Audience:      account
```

**JWT custom claims:**
```
tenantId  -> user attribute -> string
role      -> user attribute -> string  (SuperAdmin/CustomerAdmin/MillManager/Operator/Executive)
millId    -> user attribute -> string  (MillManager only)
areaIds   -> user attribute -> string[] (Operator only)
```

**Test users (password: Test@1234):**
superadmin | customeradmin | millmanager | operator | executive

**Gotchas:**
- Disable `VERIFY_PROFILE` or login is blocked when firstName/lastName missing
- `unmanagedAttributePolicy` must be `ENABLED` or custom attributes are silently dropped
- Use "User Attribute" mapper for `role`, NOT "User Realm Role"
- Re-setup: import `infrastructure/keycloak/edgepulse-realm.json`, then create 5 users manually

---

## Known Tech Debt

```
1. #28 (file attachments) skipped — needs IFileStorageService + Azure Blob/MinIO
2. DeploymentMode stored as string in DB (by design for readability)
3. AlertSeverityCode/StatusCode stored as strings (by design — processor independence)
4. CurrentUserService reads JWT claims directly — no claims caching
5. OpcUaAgent: no TLS/certificate auth (acceptable for on-premise LAN deployment)
6. Dashboard: no e2e tests yet (playwright-report/ exists from scaffolding, unused)
```

---

## Files To Know

```
project-guide.md              This file — read every session
development-setup.md        All setup commands
DOCKER-COMMANDS.md     All Docker commands
PRODUCT-ROADMAP.md     Full 22-sprint product vision
ARCHITECTURE.md        Solution architecture guide
docs/domain/           Domain reference docs (02-demo-data-setup.md)
docs/sprints/          Sprint summary docs (sprint-1 through sprint-11)
docs/keycloak-setup.md Keycloak realm + user setup guide
```
