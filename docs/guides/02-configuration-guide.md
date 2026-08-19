# EdgePulse — Configuration Guide

Every knob in the platform: application settings files, environment,
infrastructure, and the in-product configuration surfaces.

---

## 1. Configuration philosophy

Two layers, deliberately separated:

1. **Deployment configuration** — connection strings, SMTP, storage paths,
   ESG factors… lives in `appsettings.json` / environment variables and is
   set once per environment.
2. **Product configuration** — every dropdown, threshold, translation,
   webhook and branding choice is **data**, editable in the dashboard by the
   right role. Nothing domain-specific is hardcoded.

## 1a. Where secrets come from (important)

The committed `appsettings.json` files hold **placeholders** for every
credential (`<SET-VIA-USER-SECRETS-OR-ENV>`). Both .NET hosts refuse to start
while a placeholder is present and tell you which key is missing. Provide
real values through one of:

| Environment | Mechanism | Example |
|---|---|---|
| Local dev | `dotnet user-secrets` (stored outside the repo, loads only in Development) | `dotnet user-secrets set "Keycloak:ClientSecret" "…" --project src/backend/EdgePulse.API` |
| Docker / prod | Environment variables, `__` as the section separator | `ConnectionStrings__DefaultConnection`, `Keycloak__ClientSecret` |

The full one-time local set of commands is in the
[Setup guide §4](01-setup-guide.md). Never put a real credential back into
a committed `appsettings*.json`.

## 2. API (`src/backend/EdgePulse.API/appsettings.json`)

| Section | Key | Default | Meaning |
|---------|-----|---------|---------|
| ConnectionStrings | `DefaultConnection` | localhost,1433 | SQL Server (EF Core; retry-on-failure enabled) |
| ConnectionStrings | `MongoDB` | localhost:27017 | Telemetry reads (charts, energy, health) |
| MongoDB | `Database` | `edgepulse_telemetry` | Mongo database name |
| Storage | `AttachmentsRoot` | `data/attachments` | File-attachment root (mount a volume in Docker) |
| Esg | `PowerMetricKeys` | `["power_consumption"]` | Metric keys treated as instantaneous kW |
| Esg | `Co2FactorKgPerKwh` | `0.181` | Grid carbon intensity (≈EU-27; FI ≈ 0.07) |
| Keycloak | `Authority` | http://localhost:8080/realms/edgepulse | OIDC authority |
| Keycloak | `Audience`, `ClientId`, `ClientSecret` | — | JWT validation |
| Keycloak | `AdminUsername` / `AdminPassword` | admin/admin | **Dev only** — user-management admin API. Production: a service account with `manage-users` |
| Ai | `Provider` | `ollama` | `none` / `ollama` / `azureopenai` — which LLM backs the ✦ Explain feature; `none` hides it in the UI |
| Ai | `TimeoutSeconds` | 90 | Per-request model timeout (first Ollama call loads the model, ~40 s) |
| Ai | `Ollama:BaseUrl` / `Ollama:Model` | http://localhost:11434 / `llama3.2` | Local Ollama endpoint + model (`http://ollama:11434` inside the compose network) |
| Ai | `AzureOpenAi:Endpoint`, `Deployment`, `ApiKey`, `ApiVersion` | — / `gpt-4o-mini` / placeholder / `2024-10-21` | Cloud profile; `ApiKey` is a placeholder — set it via user-secrets or env |

AI env vars (Docker/prod): `Ai__Provider`, `Ai__Ollama__BaseUrl`,
`Ai__AzureOpenAi__ApiKey`. Local Azure key:
`dotnet user-secrets set "Ai:AzureOpenAi:ApiKey" "…" --project src/backend/EdgePulse.API`.
Full detail (prompts, tuning, troubleshooting): [AI guide](05-ai-guide.md).

## 3. Telemetry Processor (`src/backend/EdgePulse.TelemetryProcessor/appsettings.json`)

| Section | Key | Default | Meaning |
|---------|-----|---------|---------|
| ConnectionStrings | `RabbitMQ` / `MongoDB` / `SqlServer` | localhost | Pipeline endpoints |
| AlertEngine | `ThresholdCacheRefreshSeconds` | 60 | Threshold cache refresh |
| Smtp | `Enabled` | true | Alert e-mails on/off |
| Smtp | `Host`/`Port`/`UseSsl`/`User`/`Password` | localhost:1025 | MailHog locally; point at a real relay in production |
| Smtp | `From`, `Recipients[]` | — | Alert mail addressing |
| WorkOrders | `AutoCreateFromAlerts` | true | Auto-open a work order per qualifying alert |
| WorkOrders | `AutoCreateSeverities` | CRITICAL, HIGH | Which severities qualify |

## 4. Node services

- **Dashboard** (`src/EdgePulse.Dashboard`): dev proxy targets the API at
  `http://localhost:5104` (`vite.config.ts`); Docker image reads `API_URL`
  (nginx template). Keycloak endpoint in `src/keycloak.ts`.
- **Ingestion** (`src/EdgePulse.Ingestion`): env — `RABBITMQ_URL`,
  `API_BASE_URL` (device-key validation), `PORT`.
- **OPC-UA Agent** (`src/EdgePulse.OpcUaAgent/config/*.json`): server URL,
  publish interval and the `devices[]` metric mapping —
  **generate it with `npm run discover`** (Sprint 25).

## 5. Infrastructure (`infrastructure/docker-compose.onpremise.yml`)

All service credentials live here (defaults are development-grade:
`EdgePulse@2026` etc. — change for anything internet-facing). MailHog
captures all SMTP locally: UI at http://localhost:8025.

Optional AI services (Sprint 29): `ollama` (`ollama/ollama:0.5.7`,
container `edgepulse-ollama`, port 11434, model volume
`edgepulse_ollama_models`, `mem_limit: 4g`, healthcheck) and the one-shot
`ollama-pull` (pulls `llama3.2`, ~2 GB, once). Start them with
`docker compose -f infrastructure/docker-compose.onpremise.yml up -d ollama ollama-pull`.

## 6. In-product configuration (dashboard)

| Surface | Who | What |
|---------|-----|------|
| Configuration → Location/Device/Status/Maintenance/Metric types | Admins, MillManager | Every lookup value; system values protected, tenant overrides supported |
| Configuration → Languages / Translations | Admins | Locales, per-item translations, DB-backed UI strings, CSV round-trip |
| Configuration → Branding | Admins | White-label product name, logo, accent colour (applies live) |
| Alerts → thresholds | Admins, MillManager | Per-device metric limits, severity, consecutive-breach count |
| 👥 Users | Admins | Create users, roles, mill/area scoping, enable/disable, temp passwords |
| 🔗 Integrations | Admins | Outbound webhooks (events, HMAC secret, JSON/Slack format) |
| 🗺️ Floor Plan → Edit layout | Admins, MillManager | Device positions on the mill map |

## 7. Version / release configuration

Each component's **next version** is declared in its `CHANGELOG.md`
`## [Unreleased] — vX.Y.Z` line (CI reads it — betas publish as
`X.Y.Z-beta.N`). Releases are cut by pushing `component-vX.Y.Z` tags.
Details: [`docs/devops/02-releasing.md`](../devops/02-releasing.md).
