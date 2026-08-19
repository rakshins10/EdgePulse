# Changelog — Backend (API + Telemetry Processor)

All notable changes to the backend line are documented here. This one line covers
`edgepulse-api` **and** `edgepulse-telemetry-processor`, which share the
Domain / Application / Infrastructure libraries and are versioned together.

Format follows [Keep a Changelog](https://keepachangelog.com/);
versions follow [SemVer](https://semver.org/).

> **How versioning works.** The version on the **`## [Unreleased]`** line below is
> the *next* version. Every merge to `main` that touches the backend publishes a
> beta image tagged `<next>-beta.<N>` (N auto-increments per merge). Cutting a
> release — pushing a `backend-v<next>` tag — publishes the stable image, then you
> rename the section below to `## [<next>] — YYYY-MM-DD` and start a fresh
> `## [Unreleased]` with the next target.

## [Unreleased] — v1.2.0

_Post-1.1 development._

## [1.1.0] — 2026-08-19

The AI release (Sprints 29–30): alert explanations and grounded natural-language
Q&A with an on-premise model, plus the v1.0.1 hardening below.

### Added
- **Ask EdgePulse (Sprint 30)** — `POST /api/ai/ask`: natural-language
  questions answered by the configured LLM but grounded (RAG) in live,
  role-scoped device / alert / work-order data; deterministic device matching
  by code or name, plant-wide snapshot fallback, grounding metadata in the
  response; never throws (available:false + reason). 12 unit tests.
- **AI alert explanations (Sprint 29)** — `IAiAssistant` abstraction with
  Ollama (on-premise, `llama3.2`), Azure OpenAI and Null providers selected by
  the new `Ai` config section; `GET /api/ai/status`,
  `GET /api/ai/alerts/{id}/summary?regenerate` producing WHAT HAPPENED /
  LIKELY CAUSES / RECOMMENDED ACTION on demand, cached on `Alert.AiSummary`,
  graceful degradation. `ollama` + `ollama-pull` services in the on-prem
  compose file. 7 unit tests.

### Security
- **Secrets out of git (v1.0.1 hardening)** — `appsettings.json` now ships
  `<SET-VIA-USER-SECRETS-OR-ENV>` placeholders; API and TelemetryProcessor fail
  fast at startup with a message naming the missing key. Local: `dotnet
  user-secrets`; Docker/prod: `Section__Key` env vars. TelemetryProcessor gained
  a Development launch profile so user-secrets load under `dotnet run`.
- Redacted the Keycloak client secret from the Swagger help text.

### Fixed
- Notification bell deep-links: work-order notifications now navigate; alert
  notifications carry `?highlight=<id>` so the target row is scrolled to and
  flashed, and clicking works even when already on the target page.

## [1.0.0] — 2026-07-24

First release line. Everything built across Sprints 1–28 ships here.

### Added
- **White-label branding (Sprint 28)** — per-tenant product name / logo /
  accent colour (TenantBranding + /api/branding GET/PUT).
- **2D floor plan (Sprint 27)** — Device.FloorX/FloorY + /api/floorplan
  (mill devices with live status + alert counts; position editing for
  admins/MillManager).
- **Device health scoring (Sprint 26)** — transparent statistical condition
  score per device (alert pressure + threshold utilization + 7-day trend)
  with linear days-to-threshold estimate; /api/healthscore/devices.
- **Webhooks (Sprint 24)** — HMAC-SHA256-signed outbound webhooks
  (alert.created, workorder.created) with Slack/Teams format option,
  admin CRUD + test-fire API, delivery status tracking.
- **Audit trail (Sprint 23)** — automatic capture of every EF create/update/
  delete (property-level old→new diffs, soft-delete detection) into AuditLogs,
  admin-only audit API + CSV export.
- **Energy & ESG (Sprint 22)** — Mongo-aggregated daily energy (kWh) and CO2e
  from power telemetry, per-mill/per-device rollups, ESG CSV export; Esg config
  (PowerMetricKeys, Co2FactorKgPerKwh).
- **Maintenance work orders (Sprint 21)** — WorkOrder entity with guarded
  lifecycle (open/in-progress/on-hold/completed/cancelled), auto-creation
  from CRITICAL/HIGH alerts (config-gated, deduped per alert) + WORKORDER
  notifications, and the WorkOrders API (list/create/transition/assign).
- **User management (Sprint 20)** — Keycloak Admin REST integration
  (IIdentityAdminService/KeycloakAdminService): list/create users, role +
  mill/area scoping via user attributes, enable/disable, temp-password reset,
  with a strict admin authorization matrix.
- **Reports (Sprint 19)** — cross-mill comparison (devices, alert volumes,
  MTTA/MTTR) + CSV exports (comparison + alert detail); CsvBuilder; EF
  EnableRetryOnFailure for transient SQL faults.
- **File attachments (Sprint 18)** — IFileStorage + LocalFileStorage, upload/
  list/download/delete API for Device/Mill/Area attachments with role checks,
  extension allowlist and 25 MB limit.
- **Notifications (Sprint 17)** — `Notification` entity + Notifications API
  (list, unread count, mark read / mark all read); the TelemetryProcessor fans
  out every fired alert to an in-app notification row **and** an SMTP email
  (MailKit 4.9.0; MailHog for local dev). New `Smtp` config section.
- Clean Architecture .NET 9 solution — Domain, Application, Infrastructure, API —
  with NuGet versions pinned (EF Core 9.0.5, Swashbuckle 6.9.0).
- **Configuration module**: all lookup GET endpoints and write operations
  (device types, device statuses, metric types, alert severities/statuses,
  manufacturers, industry templates) with tenant customization and system-value
  overrides. Lookups exposed as `IQueryable<T>` to keep the Application layer free
  of EF Core.
- **Global exception-handling middleware** mapping domain exceptions to HTTP status
  codes (Validation→400, NotFound→404, Forbidden→403, Conflict→409).
- **Device registration** with API-key generation; **decommission** with API-key
  revocation.
- **Identity & auth**: JWT Bearer authentication, real `CurrentUserService`, and
  `[Authorize]` across all controllers (Keycloak, 5 roles).
- **Alerts engine**: thresholds, alert state machine, and alerts API.
- **Executive dashboard** aggregation endpoints.
- **Mill / Area / Device CRUD** — create, edit, delete, decommission.
- **Localization backend**: data-driven locales, lookup-item translations,
  DB-backed UI strings, and CSV translation round-trip (import/export).
- **Telemetry Processor** service — consumes telemetry from RabbitMQ and persists
  to MongoDB.
- **Demo data seed** with deterministic well-known GUIDs (idempotent).
- Health endpoints for API and Telemetry Processor.
- Docker images for both services and CI/CD publishing to GHCR.

### Changed
- Configuration write endpoints use separate `XxxRequest` records for reliable
  ASP.NET model binding (MediatR command records aren't bound directly).

### Fixed
- Pinned Swashbuckle to 6.9.0 — 7.x is incompatible with .NET 9.
- Registered `GuidSerializer` for MongoDB.Driver 3.x in the Telemetry Processor.
- Corrected the Telemetry Processor database name.
