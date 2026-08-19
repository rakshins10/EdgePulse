# EdgePulse — Technical Guide

Full breakdown of the architecture: components, layering, data flows and
the key design decisions. Complements
[`docs/02-architecture.md`](../02-architecture.md) (original design doc).

---

## 1. System topology

```
                 ┌────────────────────────  factory floor  ───────────────────────┐
  OPC-UA server ──► OPC-UA Agent (Node/TS) ─┐
                                            ├──► RabbitMQ ──► Telemetry Processor ──► MongoDB (time-series)
  any device ─────► Ingestion API (NestJS) ─┘   telemetry        (.NET worker)   └──► SQL: Alerts, WorkOrders,
        (X-Device-Key)                          .readings         alert engine          Notifications  + email/webhooks
                                                                                          │
  Browser ──► Dashboard (React 18 + Vite) ──► REST API (.NET 9, Clean Architecture) ──► SQL Server (EF Core)
                    │  JWT (Keycloak OIDC)          │                                └─► MongoDB (charts, energy, health)
                    └───────────────► Keycloak 24 ◄─┘  token validation + admin API
```

## 2. Backend (`src/backend/`) — Clean Architecture

| Project | Depends on | Contents |
|---------|-----------|----------|
| **Domain** | — | Entities with behaviour (guarded lifecycles, factories), no framework refs |
| **Application** | Domain | CQRS handlers (MediatR), validators (FluentValidation), interfaces (`IApplicationDbContext` as `IQueryable<T>` — no EF dependency), pure logic (`HealthMath`, `EnergyMath`, `CsvBuilder`, `WebhookSigner`) |
| **Infrastructure** | Application | `EdgePulseDbContext` (EF Core 9 + SQL Server, retry-on-failure, **automatic audit capture** in `SaveChangesAsync`), migrations, `KeycloakAdminService`, `LocalFileStorage`, `WebhookSender` |
| **API** | all | Controllers (thin → MediatR), JWT auth, exception middleware (domain exceptions → 400/403/404/409), Swagger; Mongo read-side controllers (Telemetry/Energy/HealthScore) query Mongo directly — time-series never touches EF |
| **TelemetryProcessor** | — (standalone) | RabbitMQ consumer, Mongo writer, threshold cache, alert engine, `AlertNotifier` (notifications + email + work orders) and `WebhookDispatcher`. Deliberately raw ADO.NET — a small, dependency-light worker |

**Request pipeline:** Controller → MediatR → ValidationBehaviour →
LoggingBehaviour → Handler → EF Core/Mongo → response. Domain exceptions are
mapped centrally; ill-formed requests never reach handlers.

**Multi-tenancy:** every tenant entity carries `TenantId`; handlers filter by
`ICurrentUserService.TenantId` (from JWT claims). Role guards live in
handlers, not controllers, so they are unit-tested (137 tests).

**Auditing:** `SaveChangesAsync` inspects the change tracker and writes
`AuditLogs` rows (property-level diffs, soft-delete detection) in the same
transaction.

**AI layer (Sprint 29):** follows the same layering — the LLM is a pluggable
`Infrastructure` service behind an `Application` interface.

| Layer | File | Role |
|---|---|---|
| Application | `Common/Interfaces/IAiAssistant.cs` | The contract: `IsEnabled`, `Description`, `CompleteAsync(system, user)` |
| Application | `Features/Ai/AlertSummaryPrompts.cs` | Both prompts (system + per-alert user prompt) |
| Application | `Features/Ai/GetAlertSummaryQuery.cs` | The handler: cache → enabled? → facts → model → cache |
| Infrastructure | `Services/Ai/OllamaAiAssistant.cs` | Talks to Ollama `/api/chat` (stream=false, temp 0.2, num_predict 300) |
| Infrastructure | `Services/Ai/AzureOpenAiAssistant.cs` | Same job against Azure OpenAI (cloud profile) |
| Infrastructure | `Services/Ai/NullAiAssistant.cs` | Used when `Ai:Provider = none` |
| Infrastructure | `Services/Ai/AiOptions.cs` | Binds the `Ai` config section |
| Infrastructure | `DependencyInjection.cs` | Picks the provider from `Ai:Provider` (`AddHttpClient` per provider) |
| API | `Controllers/AiController.cs` | `GET /api/ai/status`, `GET /api/ai/alerts/{id}/summary?regenerate` |
| Dashboard | `api/ai.ts`, `components/alerts/AiSummaryPanel.tsx` (+ `.module.css`), `pages/alerts/AlertsPage.tsx` | Client, panel, ✦ Explain button; i18n `ai` block in en/fi/sv |

Flow: `GET /api/ai/alerts/{id}/summary` → `AiController` → MediatR →
`GetAlertSummaryQueryHandler`: `Alert.AiSummary` set? → return (cache hit,
~60 ms) · `IAiAssistant.IsEnabled` false? → `available=false` · load device
name/type + recent readings → build prompts → `CompleteAsync` → `null`? →
`available=false` + reason · else `alert.SetAiSummary(text)` + SaveChanges.
On demand (never in the telemetry hot path), cached on the alert,
`?regenerate=true` bypasses the cache, and the handler never throws — the
alert page works unchanged when the model is down. Details, prompts and
design rationale: [AI guide](05-ai-guide.md).

## 3. Frontend (`src/EdgePulse.Dashboard`)

- **React 18 + TypeScript + Vite**; CSS Modules only (no CSS framework —
  theme via `--color-*` variables, dark/light).
- **State:** React Query for all server state (polling where liveness
  matters: bell 30 s, floor plan 10 s, boards 60 s); Redux slice only for
  the alert badge; context providers for Theme, **Toast** and **Confirm**
  (in-house accessible primitives — no native `alert/confirm`).
- **Auth:** Keycloak JS adapter; JWT claims (`role`, `tenantId`, `millId`,
  `areaIds`) drive routing/visibility (`adminOnly` nav gating).
- **i18n:** i18next with en/fi/sv bundles + DB-backed overrides loaded at
  runtime; `Accept-Language` sent on every request so server-resolved names
  localize too.
- **Build:** code-split vendor/react/charts/i18n chunks (largest ≤ ~320 kB);
  Docker image = static build behind nginx with `/api` proxy.

## 4. Identity

Keycloak 24 (realm `edgepulse`). Users carry attributes
`role/tenantId/millId/areaIds`; protocol mappers copy them into JWT claims.
The API validates via OIDC discovery; user administration goes through the
Keycloak Admin REST API (read-modify-write — a partial PUT clears profile
fields). AD/LDAP arrives via Keycloak federation, not app code
(see `docs/reference/authentication.md`).

## 5. Data stores

| Store | Data | Why |
|-------|------|-----|
| SQL Server | Org, config, alerts, work orders, notifications, audit, webhooks, branding | Relational integrity, EF migrations |
| MongoDB | `telemetry_readings` (~450k docs in demo) | Append-heavy time series; aggregation pipelines for energy/health (Guids stored as **strings**) |
| RabbitMQ | `telemetry.readings` | Decouples ingestion burst from processing |

## 6. Cross-cutting decisions worth knowing

- **3-consecutive-breach** alerting to suppress sensor noise; one open alert
  per (device, metric, threshold).
- **Everything configurable as data** — lookups, translations, thresholds,
  webhooks, branding — with system values protected and tenant overrides.
- **Best-effort side-effects**: email/webhook/work-order fan-out never blocks
  alert persistence; failures are logged and visible (delivery status).
- **Transparent analytics**: health scores and energy figures are documented
  arithmetic (the UI shows the method) — honest groundwork for future ML.
- **Versioning**: four independent release lines (backend, dashboard,
  ingestion, opcua-agent), changelog-driven betas (`X.Y.Z-beta.N`), tag-cut
  releases, images in GHCR (`docs/devops/`).

## 7. Testing

- 137 xUnit tests (Domain entity behaviour + Application handlers with an
  EF-InMemory double of `IApplicationDbContext` and NSubstitute for
  interfaces) — run in CI on every push/PR. Includes 7 AI tests
  (`Features/Ai/AlertSummaryTests.cs`) using an NSubstitute fake
  `IAiAssistant` — caching, regenerate, disabled/failure paths, prompt
  contents, tenant isolation — no model needed.
- Playwright E2E specs for CRUD/i18n flows (run locally against the stack).
- Every sprint was additionally **verified live** end-to-end; see
  `docs/sprints/` for the evidence trail.
