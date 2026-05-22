# EdgePulse — Sprint History & Project Journal

> A chronological record of what was built, why decisions were made, and what was learned.
> Written as a technical journal — readable by anyone joining the project mid-stream.
> Last updated: May 2026

---

## Project Genesis

**Date:** May 2026
**Context:** Rakshith N S, 10 years at ABB Finland building Pulp & Paper MES systems,
decided to build EdgePulse — a complete, sellable Industrial IoT platform. The goals were dual:
a career portfolio demonstrating senior/principal engineer capability, and a genuine product
that could be taken to market.

The name EdgePulse captures the two worlds: Edge (on-premise, factory floor) and Pulse
(real-time monitoring, heartbeat of the machines).

### Why Build This?

The existing market is broken for mid-size manufacturers:
- ABB Ability, Siemens MindSphere: €100k-500k/year. Mid-market can't afford it.
- Generic IoT platforms (AWS IoT, Azure IoT Hub): powerful but not industrial-specific.
  Require months of custom development. No OPC-UA out of the box. No industry templates.
- Both categories are cloud-only. Factories with no internet get nothing.

EdgePulse targets the gap: fully configurable, industry-specific templates, on-premise first,
and priced at €500-2000/month.

---

## Foundation Sprint — Documentation & Infrastructure

**Dates:** May 12-14, 2026
**Commits:** `8d79ecd` through `bfd98f3`

### Goal

Before writing a line of application code, establish the full technical foundation:
requirements, architecture, data design, and local Docker infrastructure.

### What Was Delivered

**Requirements document** (`docs/01-requirements.md`)
Five roles defined: SuperAdmin, CustomerAdmin, MillManager, Operator, Executive.
The organisational hierarchy: Platform → Tenant → Mill → Area → Device.
Feature list covering all 22 planned sprints.

**Architecture document** (`docs/02-architecture.md`)
14 sections covering everything from HAProxy load balancing to Azure OpenAI integration.
Key decision documented here: on-premise first, Clean Architecture, Keycloak for identity.
HAProxy chosen over nginx specifically for its Drain state which enables zero-downtime
rolling deployments — critical for factories that can't afford downtime.

**Data design document** (`docs/03-data-design.md`)
The configurable lookup tables design was the most important output here.
The insight: every dropdown in an industrial platform is customer-specific.
A paper mill has Digesters. A food factory has Conveyors. A wind farm has Turbines.
The solution: industry templates + tenant customisation + override layer.
This shaped the entire Sprint 1 implementation.

**Docker Compose stack** (`infrastructure/docker-compose.onpremise.yml`)
HAProxy, Keycloak+PostgreSQL, SQL Server 2022, MongoDB, RabbitMQ — all in Docker.
This gives a complete on-premise simulation locally. One `docker compose up -d` command.

### Lesson Learned

Investing in documentation first paid off immediately. When coding started, every design
decision was already resolved. Sprint 1 had zero architectural debates.

---

## Sprint 1 — Configuration Module

**Dates:** May 15-19, 2026
**Milestone:** Sprint 1 — Configuration Module
**Epic:** #1 (closed)
**Stories:** #11-#22 (all closed)
**PRs:** Multiple feature branches, all merged to main

### Goal

Build the entire configuration system that makes EdgePulse configurable. Every dropdown
in the UI reads from a database table. Tenants can add custom values and override system values.

### What Was Delivered

#### Foundation (commit `7b82241`)

.NET 9 solution scaffolded with four Clean Architecture projects. NuGet packages pinned
at specific versions — EF Core 9.0.5 and Swashbuckle 6.9.0 pinned specifically because
later versions had breaking changes. This is documented in CLAUDE.md as a hard rule.

#### Domain Layer (commit `ce247fe`)

All domain entities created. The key design here was `LookupBaseEntity` — a shared base
class for all configurable lookup tables:

```csharp
public abstract class LookupBaseEntity : BaseEntity
{
    public string Code { get; }        // e.g. "PUMP", "ONLINE", "CRITICAL"
    public bool IsSystem { get; }      // system values are protected
    public bool IsActive { get; }
    public int SortOrder { get; }
    public Guid? TenantId { get; }     // null = system, set = custom
    public Guid? TemplateId { get; }   // which industry template owns this
}
```

The `WellKnownIds` constants were defined here with the `0000000X-*` prefix strategy.
This makes seed data deterministic and idempotent.

#### Application Layer Foundation (commit `3c82707`)

`IApplicationDbContext` defined as `IQueryable<T>` properties — not `DbSet<T>`.
This was a deliberate decision to keep the Application layer free of EF Core dependencies.
Common exceptions defined. MediatR pipeline behaviours (Logging + Validation) registered.

#### First Working Endpoint (commit `ff3ea44`)

`GET /api/configuration/device-types` working end-to-end.
The MediatR pipeline was proven: Swagger → Controller → MediatR → ValidationBehaviour →
LoggingBehaviour → Handler → EF Core → SQL Server → response.

A critical bug was fixed here: `Swashbuckle` 7.x was incompatible with .NET 9.
Pinned to 6.9.0 and documented as a hard rule.

#### All GET Endpoints (commit `1428586`)

All six GET configuration endpoints added:
- device-types, device-statuses, metric-types (under Devices namespace)
- alert-severities, alert-statuses (under Alerts namespace)
- industry-templates (SuperAdmin only)

EF Core seed data added with `HasData()` in `IEntityTypeConfiguration` classes.
All system values seeded with well-known GUIDs.

**Key insight:** The `IApplicationDbContext` interface uses `IQueryable<T>` not `DbSet<T>`.
This means Application layer handlers can write LINQ queries that EF Core translates to SQL,
without Application layer depending on EF Core directly. Clean Architecture preserved.

#### Write Operations — Device Types (#12, #13, #14)

`CreateDeviceTypeCommand`, `UpdateDeviceTypeCommand`, `DeleteDeviceTypeCommand` implemented.

**Bug discovered:** Passing the Command record directly as `[FromBody]` caused Swagger 400 errors.
ASP.NET model binding couldn't reliably deserialize MediatR command records.

**Fix established as a pattern:** Separate `XxxRequest` records defined at the bottom of each
controller file. Commands are constructed from request models in the action method.
This became a project-wide standard — documented in `implementation-patterns.md`.

**Delete pattern established:** The initial delete handler queried by `TenantId` which meant
system values (TenantId = null) returned NotFoundException instead of ForbiddenException.
Fixed: check existence first, then check IsSystem, then check ownership.

#### ExceptionHandlingMiddleware (commit `1b963b8`)

Added during Sprint 1 device status write operations when testing revealed that domain
exceptions were returning 500 instead of the correct HTTP status code.

```
ValidationException  → 400
NotFoundException    → 404
ForbiddenAccess      → 403
ConflictException    → 409
Unhandled            → 500
```

This was a pivotal addition. Every subsequent story benefited from clean error responses.

#### All Remaining Lookup Types (#18, #21, #22)

Alert severity write operations, all remaining lookup types (manufacturers, metric types,
maintenance types, location types), and tenant lookup overrides completed.

**TenantLookupOverride upsert pattern:** Rather than separate Create/Update endpoints,
a PUT endpoint does upsert — if an override exists for this tenant+lookupType+lookupId,
update it; otherwise create. Simpler API surface for the consumer.

### Blockers & Pivots

1. **Swashbuckle version:** Spent time debugging 500 errors on startup before identifying
   that Swashbuckle 7.x was incompatible. Fixed by pinning to 6.9.0.

2. **Request model vs Command:** The `[FromBody] Command` pattern failed in Swagger.
   Discovered this during device type POST testing. The fix became a project-wide standard.

3. **LookupTypes.cs missing:** `UpsertTenantLookupOverrideCommand` referenced `LookupTypes`
   constants that were designed but never created. Created `LookupTypes.cs` in Domain.

### What We Learned

- Seed data with well-known GUIDs is idempotent and safe. Random GUIDs in seed data cause
  migration conflicts when the database already has data.
- The three-level lookup hierarchy (template → system → tenant) is the right model for
  a multi-tenant configurable platform. It took planning upfront but pays off every sprint.
- ExceptionHandlingMiddleware should be one of the first things built, not an afterthought.

---

## Sprint 2 — Organisation Module

**Dates:** May 20, 2026
**Milestone:** Sprint 2 — Organisation Module
**Epic:** #2 (closed)
**Stories:** #23, #24, #25, #26 (all closed)
**PR:** #46 merged

### Goal

Build the organisational hierarchy: Tenant → Mill → Area. Role-scoped access at every level.

### What Was Delivered

**CreateTenantCommand** (SuperAdmin only)
- Unique slug validation (lowercase, hyphens only, like `nordpulp-industries`)
- Optional industry template assignment at creation time
- Returns new tenant ID

**CreateMillCommand** (CustomerAdmin+)
- DeploymentMode field: Cloud or OnPremise per mill
- HasInternet flag: critical for deciding which infrastructure is used
- Code must be unique within tenant (e.g. "LW" for Lakewood)

**CreateAreaCommand** (MillManager restricted to their mill)
- MillManager can only create areas in their assigned mill
- LocationType optional (e.g. "Production Floor", "Control Room")
- Code unique within mill

**Role-scoped GET queries:**
- GetTenantsQuery: SuperAdmin sees all, others forbidden
- GetMillsQuery: SuperAdmin all, CustomerAdmin their tenant, MillManager their mill
- GetAreasQuery: adds Operator scope (only assigned areas)

### Blocker: Dev Tenant FK Violation

When testing `POST /api/organisation/mills`, the request failed with:
```
FK constraint violation: FK_Mills_Tenants_TenantId
```

The `CurrentUserService` placeholder returns `TenantId = 00000099-0000-0000-0000-000000000001`
but that tenant didn't exist in the database.

**Fix:** Manually inserted the dev tenant directly in SSMS. Also added `TenantConfiguration`
with `HasData` to seed it via migrations. But the first migration run failed because the
tenant was already in DB. Fixed by removing the `InsertData` block from the generated migration.

**Lesson:** When using `HasData` for records that may already exist in the database,
either use a conditional seed or remove the InsertData block after the initial manual insert.

### Architecture Note: DeploymentMode as String

`DeploymentMode` enum is stored as a string in the database (`"Cloud"` or `"OnPremise"`)
rather than an integer. This was a deliberate decision for readability in SQL queries
and ease of debugging. The EF Core configuration uses `.HasConversion<string>()`.

---

## Sprint 3 — Device Management (Partial)

**Dates:** May 20, 2026
**Milestone:** Sprint 3 — Device Management
**Epic:** #3 (open)
**Stories:** #27 (closed), #28 (skipped), #29 (next)

### Goal

Device registration with secure API key generation. Each registered device gets a unique
API key that it uses to authenticate telemetry submissions.

### What Was Delivered: #27 Device Registration

**DeviceApiKey entity** added to Domain:
- `KeyHash`: SHA-256 hash of the plain text key. Never stores the original.
- `KeyPrefix`: First 8 characters for UI display (e.g. `dev_a1b2`)
- `IsActive`, `ExpiresAt`, `RevokedAt`, `RevokedReason`
- `Revoke()` and `RecordUsage()` domain methods

**RegisterDeviceCommand:**
- Validates area belongs to tenant (not just any area ID)
- MillManager restricted to their assigned mill
- Device code unique within tenant (e.g. `PUMP-LW-001`)
- Generates cryptographically secure API key using `RandomNumberGenerator`
- Returns `RegisterDeviceResult` with plain text key — shown ONCE in response

**Key generation format:**
```
dev_a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5  (4-char prefix + 32 random chars)
```

**GetDevicesQuery:**
- Role-scoped (MillManager → their mill, Operator → assigned areas)
- Optional `?millId=` and `?areaId=` filters
- Returns mill name, area name, type name, status name with color
- Ordered by mill → area → code

### Story #28 Skipped

Attachment upload (#28) requires `IFileStorageService` with Azure Blob (cloud) and
MinIO (on-premise) implementations. This is a non-trivial infrastructure piece that
warrants its own sprint. Deferred to a later sprint within Sprint 3 scope.

### Next: #29 Decommission Device

The decommission story: set device status to `Decommissioned`, revoke all active API keys,
preserve all historical telemetry data. The device code should be re-usable after a
configurable retention period (to be decided).

---

## What's Next: Sprint 4 — Identity & Authentication

Sprint 4 is the most important sprint in the roadmap. Everything built so far uses a
placeholder `CurrentUserService` that returns hardcoded SuperAdmin credentials.

Sprint 4 will:
1. Set up Keycloak 24 (already running in Docker, but not integrated)
2. Build JWT middleware that reads `tenantId`, `role`, `millId`, `areaIds` from the token
3. Replace `CurrentUserService` with a real implementation reading from `IHttpContextAccessor`
4. Add `[Authorize]` attributes to controllers with role requirements
5. Set up Azure AD SSO as an identity provider in Keycloak (cloud deployment)
6. Test end-to-end: log in → get token → call API → role enforcement works

Until Sprint 4, the API is effectively open (no real authentication).
This is acceptable for local development but must be resolved before any deployment.

---

## Appendix: GitHub Milestones

| Milestone | Number | Status |
|-----------|--------|--------|
| Sprint 1 -- Configuration Module | 1 | Complete |
| Sprint 2 -- Organisation Module | 2 | Complete |
| Sprint 3 -- Device Management | 3 | In Progress |
| Sprint 4 -- Identity & Auth | 4 | Not started |
| Sprint 5 -- Telemetry Pipeline | 5 | Not started |
| Sprint 6 -- Alerts & Notifications | 6 | Not started |
| Sprint 7 -- Dashboard & Reports | 7 | Not started |
| Sprint 8 -- DevOps & CI/CD | 8 | Not started |
| Sprint 9 -- AI Features | 9 | Not started |
| Sprint 10 -- Polish & Go Live | 10 | Not started |
| Sprint 11 -- Mobile App (React Native) | 11 | Not started |
| Sprint 12 -- Predictive Maintenance (ML) | 12 | Not started |
| Sprint 13 -- Energy Monitoring & ESG | 13 | Not started |
| Sprint 14 -- OPC-UA & SCADA Integration | 14 | Not started |
| Sprint 15 -- Maintenance Work Orders | 15 | Not started |
| Sprint 16 -- Compliance & Audit Reports | 16 | Not started |
| Sprint 17 -- Multi-Language Support | 17 | Not started |
| Sprint 18 -- Digital Twin | 18 | Not started |
| Sprint 19 -- Edge AI (On-Premise ML) | 19 | Not started |
| Sprint 20 -- API Marketplace | 20 | Not started |
| Sprint 21 -- White Label & Partner Programme | 21 | Not started |
| Sprint 22 -- Commercialisation & Go To Market | 22 | Not started |

## Appendix: Closed Issues

| # | Story | Sprint |
|---|-------|--------|
| #1 | EPIC 1: Configuration Module | Sprint 1 |
| #2 | EPIC 2: Organisation Management | Sprint 2 |
| #11 | US-001: View all device types | Sprint 1 |
| #12 | US-002: Add custom device type | Sprint 1 |
| #13 | US-003: Edit custom device type | Sprint 1 |
| #14 | US-004: Deactivate custom device type | Sprint 1 |
| #15 | US-005: View all device statuses | Sprint 1 |
| #16 | US-006: Add custom device status | Sprint 1 |
| #17 | US-007: View all alert severities | Sprint 1 |
| #18 | US-008: Add custom alert severity | Sprint 1 |
| #19 | US-009: View all alert statuses | Sprint 1 |
| #20 | US-010: SuperAdmin view industry templates | Sprint 1 |
| #21 | US-011: Manage all lookup types | Sprint 1 |
| #22 | US-012: Rename/disable template values | Sprint 1 |
| #23 | US-013: SuperAdmin create tenant | Sprint 2 |
| #24 | US-014: CustomerAdmin create mill | Sprint 2 |
| #25 | US-015: MillManager create area | Sprint 2 |
| #26 | US-016: View organisation hierarchy | Sprint 2 |
| #27 | US-017: MillManager register device | Sprint 3 |
