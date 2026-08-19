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
handlers, not controllers, so they are unit-tested (149 tests).

**Auditing:** `SaveChangesAsync` inspects the change tracker and writes
`AuditLogs` rows (property-level diffs, soft-delete detection) in the same
transaction.

**AI layer (Sprints 29–30):** follows the same layering — the LLM is a pluggable
`Infrastructure` service behind an `Application` interface.

| Layer | File | Role |
|---|---|---|
| Application | `Common/Interfaces/IAiAssistant.cs` | The contract: `IsEnabled`, `Description`, `CompleteAsync(system, user)` |
| Application | `Features/Ai/AlertSummaryPrompts.cs` | Both prompts (system + per-alert user prompt) |
| Application | `Features/Ai/GetAlertSummaryQuery.cs` | The handler: cache → enabled? → facts → model → cache |
| Application | `Features/Ai/AskPrompts.cs` | Ask EdgePulse prompts (Sprint 30): system prompt + DATA-block rendering |
| Application | `Features/Ai/AskQuestionQuery.cs` | Ask handler: scoped device catalogue → focus → DATA block → model → answer + grounding |
| Infrastructure | `Services/Ai/OllamaAiAssistant.cs` | Talks to Ollama `/api/chat` (stream=false, temp 0.2, num_predict 300) |
| Infrastructure | `Services/Ai/AzureOpenAiAssistant.cs` | Same job against Azure OpenAI (cloud profile) |
| Infrastructure | `Services/Ai/NullAiAssistant.cs` | Used when `Ai:Provider = none` |
| Infrastructure | `Services/Ai/AiOptions.cs` | Binds the `Ai` config section |
| Infrastructure | `DependencyInjection.cs` | Picks the provider from `Ai:Provider` (`AddHttpClient` per provider) |
| API | `Controllers/AiController.cs` | `GET /api/ai/status`, `GET /api/ai/alerts/{id}/summary?regenerate`, `POST /api/ai/ask` |
| Dashboard | `api/ai.ts`, `components/alerts/AiSummaryPanel.tsx` (+ `.module.css`), `pages/alerts/AlertsPage.tsx` | Client, panel, ✦ Explain button; i18n `ai` block in en/fi/sv |
| Dashboard | `pages/ask/AskPage.tsx` (+ `api/ai.ts` `askQuestion()`) | ✦ Ask EdgePulse page (`/ask`): thread, example prompts, "Grounded on" line, focused-device chip; sidebar entry `nav.ask` + i18n `ask` block in en/fi/sv; device detail "✦ Ask about this device" link |

Flow: `GET /api/ai/alerts/{id}/summary` → `AiController` → MediatR →
`GetAlertSummaryQueryHandler`: `Alert.AiSummary` set? → return (cache hit,
~60 ms) · `IAiAssistant.IsEnabled` false? → `available=false` · load device
name/type + recent readings → build prompts → `CompleteAsync` → `null`? →
`available=false` + reason · else `alert.SetAiSummary(text)` + SaveChanges.
On demand (never in the telemetry hot path), cached on the alert,
`?regenerate=true` bypasses the cache, and the handler never throws — the
alert page works unchanged when the model is down. Details, prompts and
design rationale: [AI guide](05-ai-guide.md).

**Ask EdgePulse (Sprint 30) — retrieval-then-generate (RAG) over scoped
data.** `POST /api/ai/ask` → `AiController` → MediatR →
`AskQuestionQueryHandler`: (1) build the device catalogue the caller may see
(tenant + role scoping: MillManager → their mill, Operator → their areas);
(2) pick the focus — an explicit `deviceId` (404 if not visible), else
device codes/names mentioned in the question (deterministic string match,
max 3), else a tenant-wide snapshot; (3) render a compact plain-text DATA
block — per device: type/status/mill/area/last seen/installed, alerts
(last 30 days + any still open) with a severity breakdown and the latest 5,
open work orders; plant snapshot: open alerts by severity + latest 8,
top-3 devices by alerts in the last 7 days, open work orders; (4) the
system prompt (`AskPrompts.cs`) tells the model to answer **only** from
DATA, say what is missing, cite device name + code, stay under ~150 words
and hedge; (5) the response carries `grounding` (devices, alert and
work-order counts, scope) so the UI can show what the answer rests on.
Nothing is cached or written; validation (empty / > 500 chars → 400) and
the disabled/failed-model paths (`available:false` + `reason`) mirror the
alert summary. Why RAG rather than tool-calling: small local models (3B)
handle tool use poorly, whereas retrieve-then-generate is deterministic on
the retrieval side, cheap and unit-testable without a model.

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

- 149 xUnit tests (30 Domain entity behaviour + 119 Application handlers
  with an EF-InMemory double of `IApplicationDbContext` and NSubstitute for
  interfaces) — run in CI on every push/PR. Includes 7 AI tests
  (`Features/Ai/AlertSummaryTests.cs`) using an NSubstitute fake
  `IAiAssistant` — caching, regenerate, disabled/failure paths, prompt
  contents, tenant isolation — and 12 Ask EdgePulse tests
  (`Features/Ai/AskQuestionTests.cs`) — grounding content, role scoping,
  device matching, validation, disabled/null-answer paths — no model needed.
- Playwright E2E specs for CRUD/i18n flows (run locally against the stack),
  including `e2e/sprint30-ask.spec.ts` (3 tests) for the Ask page.
- Every sprint was additionally **verified live** end-to-end; see
  `docs/sprints/` for the evidence trail.
