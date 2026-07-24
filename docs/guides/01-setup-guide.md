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

Wait until `docker compose … ps` shows the databases healthy.

## 4. Create the database schema + demo data

```bash
# apply EF Core migrations
dotnet ef database update \
  --project src/backend/EdgePulse.Infrastructure \
  --startup-project src/backend/EdgePulse.API

# seed the NordPulp demo (tenants, mills, areas, 20+ devices, thresholds)
dotnet run --project src/backend/EdgePulse.API -- --seed
```

## 5. Keycloak one-time setup

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

## 6. Run the backend

Two terminals (or use `--configuration Release`):

```bash
# Terminal 1 — REST API  → http://localhost:5104 (Swagger at /swagger)
dotnet run --project src/backend/EdgePulse.API

# Terminal 2 — Telemetry Processor (RabbitMQ → MongoDB + alert engine)
dotnet run --project src/backend/EdgePulse.TelemetryProcessor
```

Expect: API `{"status":"healthy"}` at `/health`; the processor logs
`TelemetryProcessor ready. Listening on queue 'telemetry.readings'.`

## 7. Run the dashboard

```bash
cd src/EdgePulse.Dashboard
npm install
npm run dev          # → http://localhost:3000
```

Sign in with a demo user (e.g. `superadmin` / `Test@1234`).

## 8. Verify the whole pipeline

1. **Telemetry** — Devices → any device: live charts update every 10 s
   (simulator → agent → RabbitMQ → processor → MongoDB → API).
2. **Alerts** — the simulator breaches thresholds occasionally; watch the
   sidebar Alerts badge, the 🔔 notification bell and MailHog
   (http://localhost:8025) for the alert email.
3. **Work orders** — a HIGH/CRITICAL alert auto-opens one under 🛠️ Work Orders.
4. **Energy** — ⚡ Energy & ESG shows kWh/CO₂ for the refiners.

## 9. Everyday commands

```bash
# build + tests (backend: 130 unit tests)
dotnet build src/backend/EdgePulse.sln -c Release
dotnet test  src/backend/EdgePulse.sln

# dashboard production build
cd src/EdgePulse.Dashboard && npm run build

# OPC-UA auto-discovery against any server
cd src/EdgePulse.OpcUaAgent && npm run discover -- opc.tcp://host:4840

# stop everything
docker compose -f infrastructure/docker-compose.onpremise.yml down
```

## 10. Troubleshooting quick hits

| Symptom | Fix |
|---------|-----|
| Port already in use / HTTP 000 from a container | `docker restart <container>` — Docker Desktop's port relay can wedge after sleep/restart |
| `MSB3021/3027` file-lock build errors | Stop the running API/Processor first (they hold Release DLLs) |
| Dashboard shows zeros after login | Keycloak protocol mappers missing (step 5.2) |
| SQL "pre-login handshake" 500s | Transient after container restart — EF retries automatically; persistent → restart `edgepulse-sqlserver` |
| No telemetry charts | Is `edgepulse-opcua-agent` running? Check RabbitMQ UI (15672) queue `telemetry.readings` |

More: [`docs/reference/operations.md`](../reference/operations.md).
