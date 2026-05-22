# CLAUDE.md — EdgePulse Project Memory

> This file is read automatically by Claude Code on every session.
> Keep it updated. It is the single source of truth for project context.
> Last updated: May 2026 | Sprint 3 in progress

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
Telemetry:        Node.js 20, NestJS, TypeScript  [Sprint 5 - NOT YET BUILT]
Processor:        .NET 9 Worker Service            [Sprint 5 - NOT YET BUILT]
Identity:         Keycloak 24 + PostgreSQL 16      [Sprint 4 - NOT YET BUILT]
Message Queue:    Azure Service Bus (cloud) / RabbitMQ (on-premise)
Telemetry DB:     Azure Cosmos DB (cloud) / MongoDB (on-premise)
Primary DB:       Azure SQL (cloud) / SQL Server 2022 (on-premise, Docker)
Load Balancer:    HAProxy (on-premise) / Azure Container Apps (cloud)
AI:               Azure OpenAI GPT-4o-mini (cloud) / Ollama llama3.2 (on-premise)
ML:               Azure ML (cloud) / ONNX runtime (on-premise)
Mobile:           React Native (iOS + Android)     [Sprint 11 - NOT YET BUILT]
Frontend:         React 18, TypeScript, Tailwind   [Sprint 7 - NOT YET BUILT]
CI/CD:            GitHub Actions + self-hosted runner [Sprint 8 - NOT YET BUILT]
```

**CRITICAL PACKAGE PINS — DO NOT UPGRADE:**
- EF Core: `9.0.5` (9.0.6+ breaks migrations)
- Swashbuckle: `6.9.0` (7.x breaks .NET 9 Swagger)
- JWT Bearer: `9.0.5`

---

## Solution Structure

```
src/EdgePulse.sln
├── EdgePulse.Domain/                    # No dependencies. Pure C#.
│   ├── Common/
│   │   ├── BaseEntity.cs               # Id, CreatedAt, UpdatedAt, IsDeleted, DeletedAt
│   │   ├── TenantBaseEntity.cs         # + TenantId
│   │   └── LookupBaseEntity.cs         # + Code, IsSystem, IsActive, SortOrder, TemplateId
│   ├── Entities/                       # All domain entities
│   ├── Enums/                          # UserRole, DeploymentMode, RoleScope
│   └── Constants/
│       ├── WellKnownIds.cs             # System GUIDs (00000010-..., 00000011-..., etc.)
│       ├── LookupTypes.cs              # "DeviceType", "DeviceStatus", etc.
│       ├── EntityTypes.cs
│       └── FileCategories.cs
│
├── EdgePulse.Application/               # Depends on Domain only.
│   ├── Common/
│   │   ├── Interfaces/
│   │   │   ├── IApplicationDbContext.cs # IQueryable<T> for each entity
│   │   │   ├── ICurrentUserService.cs
│   │   │   └── IFileStorageService.cs
│   │   ├── Exceptions/
│   │   │   ├── ValidationException.cs   # -> 400
│   │   │   ├── NotFoundException.cs     # -> 404
│   │   │   ├── ForbiddenAccessException.cs # -> 403
│   │   │   └── ConflictException.cs     # -> 409
│   │   └── Behaviours/
│   │       ├── ValidationBehaviour.cs   # MediatR pipeline: FluentValidation
│   │       └── LoggingBehaviour.cs      # MediatR pipeline: request/response logging
│   └── Features/
│       ├── Devices/
│       │   ├── Commands/               # Write operations
│       │   └── Queries/                # Read operations (never mutate state)
│       ├── Alerts/
│       │   ├── Commands/
│       │   └── Queries/
│       ├── Tenants/
│       │   ├── Commands/
│       │   └── Queries/
│       ├── Mills/
│       │   ├── Commands/
│       │   └── Queries/
│       └── Areas/
│           ├── Commands/
│           └── Queries/
│
├── EdgePulse.Infrastructure/            # Depends on Application + Domain.
│   └── Persistence/
│       ├── EdgePulseDbContext.cs        # EF Core DbContext
│       ├── Configurations/             # IEntityTypeConfiguration<T> per entity
│       └── Migrations/                 # EF Core migrations
│
└── EdgePulse.API/                       # Depends on Application + Infrastructure.
    ├── Controllers/
    │   ├── ConfigurationController.cs
    │   ├── OrganisationController.cs
    │   └── DevicesController.cs
    ├── Middleware/
    │   └── ExceptionHandlingMiddleware.cs
    └── Program.cs
```

---

## Domain Model

### Organisational Hierarchy

```
EdgePulse Platform (SuperAdmin)
  └── Tenant (e.g. NordPulp Industries)
        └── Mill (e.g. Lakewood Mill, Tampere)
              └── Area (e.g. Paper Machine 1)
                    └── Device (e.g. PUMP-LW-001)
```

### Roles (5 total)

| Role | Scope | Can Do |
|------|-------|--------|
| SuperAdmin | Platform | All tenants, all mills, create tenants |
| CustomerAdmin | Tenant | All mills within tenant |
| MillManager | Single Mill | One mill, create areas and devices |
| Operator | Assigned Areas | Read + acknowledge alerts |
| Executive | Tenant (read-only) | Dashboard and reports only |

### Key Entities

```
Tenant           -> Name, Slug (unique), ContactEmail, Status
Mill             -> TenantId, Name, Code, Location, Timezone, HasInternet, DeploymentMode
Area             -> TenantId, MillId, Name, Code, LocationTypeId, Description
Device           -> TenantId, MillId, AreaId, TypeId, StatusId, Name, Code, SerialNumber
DeviceApiKey     -> TenantId, DeviceId, KeyHash (SHA-256), KeyPrefix, IsActive
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
- `00000099-*` = Dev/test tenant (placeholder until Keycloak)

---

## Coding Standards (NEVER VIOLATE)

### 1. No Hardcoded Values
```csharp
// WRONG
if (status == "ONLINE") { ... }

// CORRECT
if (status == GenericDeviceStatusIds.Online.ToString()) { ... }
// or use the constant directly in queries
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
// Command = write operation (INSERT, UPDATE, DELETE)
public record CreateDeviceTypeCommand(...) : IRequest<Guid>;

// Query = read operation (SELECT only, never mutates state)
public record GetDeviceTypesQuery() : IRequest<List<DeviceTypeDto>>;
```

### 4. IApplicationDbContext — IQueryable Pattern
```csharp
// Interface uses IQueryable, not DbSet
IQueryable<DeviceType> DeviceTypes { get; }

// Never expose EF-specific types in Application layer
// Global Query Filters handle TenantId + IsDeleted automatically
```

### 5. Global Query Filters (auto-applied in EF Core)
```csharp
// DeviceType has TenantId filter + IsDeleted filter applied globally
// You never need to write: .Where(x => !x.IsDeleted && x.TenantId == ...)
// The filters are in EdgePulseDbContext.OnModelCreating()
```

### 6. Soft Delete Only
```csharp
// NEVER: _context.Remove(entity)
// ALWAYS:
entity.Deactivate(); // sets IsDeleted = true, DeletedAt = UtcNow
_context.Update(entity);
await _context.SaveChangesAsync(cancellationToken);
```

### 7. Request Models for POST/PUT (not Commands directly)
```csharp
// WRONG — causes Swagger 400 errors
public async Task<IActionResult> CreateDeviceType(
    [FromBody] CreateDeviceTypeCommand command, ...)

// CORRECT — separate request model
public async Task<IActionResult> CreateDeviceType(
    [FromBody] CreateDeviceTypeRequest request, ...)
{
    var id = await _mediator.Send(new CreateDeviceTypeCommand(
        request.Name, request.Code, ...), cancellationToken);
}
```

### 8. All Timestamps UTC
```csharp
CreatedAt = DateTime.UtcNow  // always, never DateTime.Now
```

### 9. Exception Pattern
```csharp
// Domain exceptions map to HTTP via ExceptionHandlingMiddleware
throw new NotFoundException(nameof(DeviceType), request.Id);    // -> 404
throw new ForbiddenAccessException();                            // -> 403
throw new ConflictException("Code already exists.");             // -> 409
// ValidationException is thrown automatically by ValidationBehaviour -> 400
```

### 10. Delete Pattern (check existence before IsSystem)
```csharp
// CORRECT order:
var entity = await _context.X
    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct)
    ?? throw new NotFoundException(...);     // 1. Check exists

if (entity.IsSystem)
    throw new ForbiddenAccessException();    // 2. Check system

if (entity.TenantId != _currentUser.TenantId)
    throw new ForbiddenAccessException();    // 3. Check ownership

// 4. Check in-use (where applicable)
```

---

## Local Infrastructure

```bash
# Start all services
cd /c/Studies/EdgePulse-Application/EdgePulse
docker compose -f infrastructure/docker-compose.onpremise.yml up -d

# Stop
docker compose -f infrastructure/docker-compose.onpremise.yml down

# Status
docker compose -f infrastructure/docker-compose.onpremise.yml ps
```

| Service | URL | Credentials |
|---------|-----|-------------|
| Swagger | http://localhost:5104/swagger | n/a |
| HAProxy Stats | http://localhost:8404/stats | admin/edgepulse123 |
| Keycloak | http://localhost:8080 | admin/admin |
| RabbitMQ UI | http://localhost:15672 | edgepulse/EdgePulse@2026 |
| SQL Server | localhost:1433 | sa/EdgePulse@2026 |
| MongoDB | localhost:27017 | edgepulse/EdgePulse@2026 |
| PostgreSQL | localhost:5432 | keycloak/keycloak |

---

## EF Core Migrations

```bash
cd /c/Studies/EdgePulse-Application/EdgePulse/src

# Add migration
dotnet ef migrations add <MigrationName> \
  --project EdgePulse.Infrastructure \
  --startup-project EdgePulse.API

# Apply to database
dotnet ef database update \
  --project EdgePulse.Infrastructure \
  --startup-project EdgePulse.API

# Remove last migration (if not applied)
dotnet ef migrations remove \
  --project EdgePulse.Infrastructure \
  --startup-project EdgePulse.API
```

**IMPORTANT:** If migration has `InsertData` for a record already in DB,
manually remove the `InsertData` block from the migration file before running update.

---

## GitHub Workflow

```bash
# Fix gh CLI PATH (run once per terminal session, or add to ~/.bashrc)
export PATH=$PATH:"/c/Program Files/GitHub CLI"

# Permanent fix (already done)
echo 'export PATH=$PATH:"/c/Program Files/GitHub CLI"' >> ~/.bashrc
```

### Per-Story Workflow

```
1. Pick story from Backlog on GitHub Project board
2. Move to "In Progress"
3. git checkout -b feature/US-XXX-short-description
4. Code + commit: git commit -m "feat: description #XX"
5. Test in Swagger
6. Move to "In Review" on board
7. git push origin feature/US-XXX-...
8. gh pr create --title "..." --body "... Closes #XX ..."
9. Merge PR on GitHub
10. Issue auto-closes, board moves to Done
11. git checkout main && git pull && git branch -d feature/US-XXX-...
```

### Commit Message Format

```
feat:     new feature
fix:      bug fix
docs:     documentation only
infra:    infrastructure / docker / config
test:     tests
chore:    maintenance
refactor: restructure

Always include issue reference: "feat: description #XX"
"Closes #XX" in PR body auto-closes the issue.
```

---

## Current Sprint Status

### DONE

| Sprint | Epic | Stories |
|--------|------|---------|
| Sprint 1 | Config Module (#1 closed) | #11-#22 all closed |
| Sprint 2 | Organisation Module (#2 closed) | #23-#26 all closed |
| Sprint 3 (partial) | Device Management (#3 open) | #27 closed |

### IN PROGRESS

```
Sprint 3: Device Management
  #28 Upload attachments     SKIPPED (needs file storage service)
  #29 Decommission device    NEXT → branch: feature/US-029-decommission-device
```

### NEXT SPRINTS

```
Sprint 4:  Keycloak JWT authentication (most important — secures everything)
Sprint 5:  Telemetry pipeline (NestJS ingestion + Processor worker)
Sprint 6:  Alerts & notifications
Sprint 7:  React dashboard
Sprint 8:  CI/CD pipeline
Sprint 9:  AI features
Sprint 10: Polish + demo environment
```

---

## Completed API Endpoints

### ConfigurationController (`/api/configuration/`)

```
GET    device-types                  Returns system + tenant custom types
POST   device-types                  Create custom type
PUT    device-types/{id}             Update custom type (not system)
DELETE device-types/{id}             Deactivate custom type

GET    device-statuses               Returns system + custom statuses
POST   device-statuses               Create custom status
PUT    device-statuses/{id}          Update custom status
DELETE device-statuses/{id}          Deactivate custom status

GET    metric-types                  Returns system + custom metrics
POST   metric-types                  Create custom metric type

GET    alert-severities              Returns severities ordered by priority
POST   alert-severities              Create custom severity
PUT    alert-severities/{id}         Update custom severity
DELETE alert-severities/{id}         Deactivate custom severity

GET    alert-statuses                Returns statuses with IsTerminal flag

GET    industry-templates            SuperAdmin only

GET    manufacturers                 Returns system + custom manufacturers
POST   manufacturers                 Create custom manufacturer

POST   device-models                 Create device model under manufacturer

GET    maintenance-types             Returns system + custom maintenance types
POST   maintenance-types             Create custom maintenance type

GET    location-types                Returns system + custom location types
POST   location-types                Create custom location type

GET    lookup-overrides              Tenant overrides (filter by ?lookupType=)
PUT    lookup-overrides              Upsert override (rename or disable)
DELETE lookup-overrides/{id}         Remove override (restore default)
```

### OrganisationController (`/api/organisation/`)

```
GET    tenants                       SuperAdmin only
POST   tenants                       Create tenant (SuperAdmin only)

GET    mills                         Role-scoped
POST   mills                         Create mill (CustomerAdmin+)

GET    areas                         Role-scoped, ?millId= filter
POST   areas                         Create area (MillManager restricted to their mill)
```

### DevicesController (`/api/devices/`)

```
GET    devices                       Role-scoped, ?millId= ?areaId=
POST   devices                       Register device, returns API key ONCE
```

---

## Placeholder — CurrentUserService

Until Keycloak JWT is implemented (Sprint 4), `CurrentUserService` returns hardcoded values:

```csharp
UserId   = "dev-user-001"
TenantId = Guid.Parse("00000099-0000-0000-0000-000000000001")
Role     = UserRole.SuperAdmin
IsSuperAdmin = true
```

The dev tenant `00000099-0000-0000-0000-000000000001` is manually seeded in SQL Server.
**Replace entirely when Keycloak is implemented.**

---

## Known Issues / Tech Debt

```
1. Duplicate using in UpsertTenantLookupOverrideCommand.cs (CS0105 warning) — harmless
2. CurrentUserService is a placeholder — no real auth until Sprint 4
3. #28 (file attachments) skipped — needs IFileStorageService + Azure Blob/MinIO
4. DeploymentMode enum stored as string in DB (by design for readability)
5. Dev tenant seeded manually — will be handled by migration after Sprint 4
```

---

## Files To Know

```
CLAUDE.md              This file — read every session
CLAUDE-SETUP.md        All setup commands
DOCKER-COMMANDS.md     All Docker commands
PRODUCT-ROADMAP.md     Full 22-sprint product vision
ARCHITECTURE.md        Solution architecture guide
docs/01-requirements.md
docs/02-architecture.md
docs/03-data-design.md
docs/sprint-history.md
docs/implementation-patterns.md
```
