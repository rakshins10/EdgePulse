# EdgePulse — Setup Guide

Step-by-step instructions to install and run the complete platform from a
fresh clone. Target: Windows / macOS / Linux with Docker.

---

## 1. Prerequisites

| Tool | Version | Used for |
|------|---------|----------|
| .NET SDK | 9.0.x | API + Telemetry Processor |
| Node.js | 20.x | Dashboard, Ingestion, OPC-UA Agent |
| Docker Desktop | current | Infrastructure containers |
| Git | current | Source |

Verify: `dotnet --version`, `node --version`, `docker --version`.

## 2. Clone

```bash
git clone https://github.com/rakshins10/EdgePulse.git
cd EdgePulse
```

## 3. Start the infrastructure (Docker)

```bash
docker compose -f infrastructure/docker-compose.onpremise.yml up -d
```

This starts (first run downloads images — allow a few minutes):

| Container | Purpose | Host port |
|-----------|---------|-----------|
| `edgepulse-sqlserver` | SQL Server 2022 — primary DB | 1433 |
| `edgepulse-mongodb` | MongoDB 7 — telemetry time-series | 27017 |
| `edgepulse-rabbitmq` | RabbitMQ — telemetry queue (+ UI) | 5672 / 15672 |
| `edgepulse-postgres` | PostgreSQL — Keycloak's DB | 5432 |
| `edgepulse-keycloak` | Keycloak 24 — identity | 8080 |
| `edgepulse-mailhog` | Local SMTP catcher (alert emails) | 1025 / **8025 UI** |
| `edgepulse-opcua-simulator` | Simulated OPC-UA plant (NordPulp) | 4840 |
| `edgepulse-opcua-agent` | Edge agent publishing telemetry | — |
| `edgepulse-ingestion` | REST telemetry ingestion (NestJS) | 3000* |
| `edgepulse-haproxy` | Load balancer (stats UI) | 8404 |
| `edgepulse-ollama` | Ollama — local LLM for AI alert explanations (**optional**, Sprint 29) | 11434 |

Wait until `docker compose … ps` shows the databases healthy.

### 3a. (Optional) Start Ollama and fetch the model — AI alert explanations

The **✦ Explain** button on the Alerts page uses a local LLM served by
Ollama (`ollama/ollama:0.5.7`). The platform runs fine without it — with no
model the button simply does not appear. To enable it:

```bash
docker compose -f infrastructure/docker-compose.onpremise.yml up -d ollama ollama-pull
```

First time: ~1 GB Ollama image + **~2 GB llama3.2 download** (once; kept in
the `edgepulse_ollama_models` volume). `ollama-pull` is a one-shot helper
that pulls `llama3.2` and exits. Watch progress:
`docker logs -f edgepulse-ollama-pull`. Ready when:

```bash
curl http://localhost:11434/api/tags        # → lists llama3.2
```

**RAM:** Ollama is capped at 4 GB (`mem_limit`). Docker Desktop's VM needs
≥ 6–8 GB total for the full stack + model; stop other Docker projects if
answers time out. Full explanation, config and tuning:
[AI guide](05-ai-guide.md).

## 4. Configure application secrets (one time)

The committed `appsettings.json` files contain **placeholders**, not real
credentials — the API and Telemetry Processor refuse to start until they are
set (you get a clear message naming the missing key). For local development
use `dotnet user-secrets`, which stores values outside the repo:

```bash
API=src/backend/EdgePulse.API/EdgePulse.API.csproj
TP=src/backend/EdgePulse.TelemetryProcessor/EdgePulse.TelemetryProcessor.csproj

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=EdgePulse;User Id=sa;Password=EdgePulse@2026;TrustServerCertificate=True;" --project $API
dotnet user-secrets set "ConnectionStrings:MongoDB" "mongodb://edgepulse:EdgePulse%402026@localhost:27017" --project $API
dotnet user-secrets set "Keycloak:ClientSecret" "<edgepulse-api client secret from Keycloak>" --project $API
dotnet user-secrets set "Keycloak:AdminPassword" "admin" --project $API

dotnet user-secrets set "ConnectionStrings:SqlServer" "Server=localhost,1433;Database=EdgePulse;User Id=sa;Password=EdgePulse@2026;TrustServerCertificate=True" --project $TP
dotnet user-secrets set "ConnectionStrings:MongoDB"   "mongodb://edgepulse:EdgePulse%402026@localhost:27017" --project $TP
dotnet user-secrets set "ConnectionStrings:RabbitMQ"  "amqp://edgepulse:EdgePulse%402026@localhost:5672/edgepulse" --project $TP
```

These match the dev-grade credentials in `docker-compose.onpremise.yml`.
The Keycloak client secret is under Clients → `edgepulse-api` → Credentials.

> **Production / Docker:** do not use user-secrets — set the same keys as
> environment variables with double-underscore separators, e.g.
> `ConnectionStrings__DefaultConnection`, `Keycloak__ClientSecret`. Both hosts
> read them automatically. Details: [Configuration guide §2](02-configuration-guide.md).

## 5. Create the database schema + demo data

```bash
# apply EF Core migrations
dotnet ef database update \
  --project src/backend/EdgePulse.Infrastructure \
  --startup-project src/backend/EdgePulse.API

# seed the NordPulp demo (tenants, mills, areas, 20+ devices, thresholds)
dotnet run --project src/backend/EdgePulse.API -- --seed
```

## 6. Keycloak one-time setup

Keycloak dev-imports the `edgepulse` realm; verify at
http://localhost:8080 (admin / admin):

1. Realm **edgepulse** exists with clients `edgepulse-api` and
   `edgepulse-dashboard`.
2. The `edgepulse-dashboard` client has **protocol mappers** for the user
   attributes `tenantId`, `role`, `millId`, `areaIds`, `email` — without
   them JWTs lack the claims and the dashboard shows zeros.
3. Demo users exist (`superadmin`, `customeradmin`, `millmanager`,
   `operator`, `executive`). Set a password on the **Credentials** tab
   (e.g. `Test@1234`, *Temporary: OFF*).

Full details: [`docs/reference/authentication.md`](../reference/authentication.md).

## 7. Run the backend

Two terminals (or use `--configuration Release`):

```bash
# Terminal 1 — REST API  → http://localhost:5104 (Swagger at /swagger)
dotnet run --project src/backend/EdgePulse.API

# Terminal 2 — Telemetry Processor (RabbitMQ → MongoDB + alert engine)
dotnet run --project src/backend/EdgePulse.TelemetryProcessor
```

Expect: API `{"status":"healthy"}` at `/health`; the processor logs
`TelemetryProcessor ready. Listening on queue 'telemetry.readings'.`

## 8. Run the dashboard

```bash
cd src/EdgePulse.Dashboard
npm install
npm run dev          # → http://localhost:3000
```

Sign in with a demo user (e.g. `superadmin` / `Test@1234`).

## 9. Verify the whole pipeline

1. **Telemetry** — Devices → any device: live charts update every 10 s
   (simulator → agent → RabbitMQ → processor → MongoDB → API).
2. **Alerts** — the simulator breaches thresholds occasionally; watch the
   sidebar Alerts badge, the 🔔 notification bell and MailHog
   (http://localhost:8025) for the alert email.
3. **Work orders** — a HIGH/CRITICAL alert auto-opens one under 🛠️ Work Orders.
4. **Energy** — ⚡ Energy & ESG shows kWh/CO₂ for the refiners.
5. **AI explanations** (if Ollama is running, step 3a) — Alerts → any row →
   **✦ Explain**: the panel fills with WHAT HAPPENED / LIKELY CAUSES /
   RECOMMENDED ACTION. The first call takes ~40 s while the model loads;
   later calls 5–15 s, and repeat views are served from cache.

## 10. Everyday commands

```bash
# build + tests (backend: 137 unit tests)
dotnet build src/backend/EdgePulse.sln -c Release
dotnet test  src/backend/EdgePulse.sln

# dashboard production build
cd src/EdgePulse.Dashboard && npm run build

# OPC-UA auto-discovery against any server
cd src/EdgePulse.OpcUaAgent && npm run discover -- opc.tcp://host:4840

# stop everything
docker compose -f infrastructure/docker-compose.onpremise.yml down
```

## 11. Troubleshooting quick hits

| Symptom | Fix |
|---------|-----|
| Port already in use / HTTP 000 from a container | `docker restart <container>` — Docker Desktop's port relay can wedge after sleep/restart |
| `MSB3021/3027` file-lock build errors | Stop the running API/Processor first (they hold Release DLLs) |
| Service exits with `… is not configured. Set it via dotnet user-secrets` | Step 4 not done (or running as Production without env vars) — run the user-secrets commands, or set the `Section__Key` env vars |
| Dashboard shows zeros after login | Keycloak protocol mappers missing (step 6.2) |
| SQL "pre-login handshake" 500s | Transient after container restart — EF retries automatically; persistent → restart `edgepulse-sqlserver` |
| No telemetry charts | Is `edgepulse-opcua-agent` running? Check RabbitMQ UI (15672) queue `telemetry.readings` |
| Keycloak "port 8080 already allocated" / 404 on the realm | Another Docker project (e.g. another stack's keycloak or React app) holds 8080/3000 — `docker ps` to find it, stop it, then `up -d` again |
| Keycloak crash-loops with "Failed to obtain JDBC connection" after `--force-recreate` | Recreating keycloak alone can bring it up with no network attached — recreate it together with its DB: `docker compose -f infrastructure/docker-compose.onpremise.yml up -d --force-recreate postgres keycloak` |
| AI panel says "did not return a summary" | First call loads the model (~40 s) — click Retry; check `docker ps` for `edgepulse-ollama`; raise `Ai:TimeoutSeconds`. More: [AI guide §3.6](05-ai-guide.md) |

More: [`docs/reference/operations.md`](../reference/operations.md).
