# EdgePulse — Local End-to-End Test Guide

**Last updated:** 2026-05-31  
**Target:** Full NordPulp demo — all 20 devices generating telemetry, alerts firing, dashboard live.

---

## Prerequisites

| Tool | Version | Check |
|------|---------|-------|
| Docker Desktop | Any recent | `docker --version` |
| .NET SDK | 9.x | `dotnet --version` |
| Node.js | 20.x | `node --version` |
| npm | 10.x+ | `npm --version` |

---

## Architecture in Local Dev

```
Browser
  |
  v
Vite Dev Server :5173
  |-- /api/*  -->  EdgePulse.API :5104  -->  SQL Server :1433
  |                                     -->  Keycloak   :8080
  |
  v
RabbitMQ :5672  <--  OPC-UA Agent (Docker)  <--  OPC-UA Simulator (Docker)
  |
  v
TelemetryProcessor  -->  MongoDB    :27017
                    -->  SQL Server :1433  (alerts)
```

**What runs where:**

| Service | Runs in | Port |
|---------|---------|------|
| SQL Server | Docker | 1433 |
| MongoDB | Docker | 27017 |
| RabbitMQ | Docker | 5672, 15672 |
| Keycloak | Docker | 8080 |
| OPC-UA Simulator | Docker | 4840 |
| OPC-UA Agent | Docker | — |
| EdgePulse.API | Local (`dotnet run`) | 5104 |
| TelemetryProcessor | Local (`dotnet run`) | — |
| Dashboard | Local (`npm run dev`) | 5173 |

> The **Ingestion service** (HTTP device push path) is not needed for the OPC-UA demo.
> The OPC-UA Agent publishes telemetry directly to RabbitMQ, bypassing Ingestion.

---

## Step 1 — Start Infrastructure

```powershell
cd C:\Studies\EdgePulse-Application\EdgePulse

docker compose -f infrastructure/docker-compose.onpremise.yml up -d `
    sqlserver mongodb rabbitmq postgres keycloak
```

Wait for all containers to be healthy (~60s for Keycloak):

```powershell
docker compose -f infrastructure/docker-compose.onpremise.yml ps
```

All containers should show `(healthy)`. Keycloak may take 60–90 seconds.

**Verify connections:**

| Service | URL / Command | Expected |
|---------|---------------|----------|
| SQL Server | `sqlcmd -S localhost,1433 -U sa -P "EdgePulse@2026" -Q "SELECT 1"` | `1` |
| MongoDB | `mongosh "mongodb://edgepulse:EdgePulse%402026@localhost:27017" --eval "db.adminCommand('ping')"` | `ok: 1` |
| RabbitMQ UI | http://localhost:15672 (edgepulse / EdgePulse@2026) | Login works |
| Keycloak | http://localhost:8080 (admin / admin) | Realm list visible |

---

## Step 2 — Run EF Migrations + Demo Seed

```powershell
cd C:\Studies\EdgePulse-Application\EdgePulse

# Apply EF Core migrations (creates the EdgePulse database + all tables)
dotnet run --project src/EdgePulse.API -- --seed
```

Expected output:
```
Applying migrations...
Seeding demo data...
  Tenant: NordPulp (10000001-...)
  Mill: Lakewood Mill
  Mill: Riverside Mill
  ... (devices + thresholds)
Demo seed complete. Exiting.
```

> **Idempotent**: safe to run multiple times — existing data is not duplicated.

---

## Step 3 — Start the API

```powershell
dotnet run --project src/EdgePulse.API
```

Expected output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5104
info: Microsoft.Hosting.Lifetime[0]
      Application started.
```

**Verify:**
- Health: http://localhost:5104/health → `{"status":"healthy"}`
- Swagger: http://localhost:5104/swagger → API explorer (JWT auth required for most endpoints)

---

## Step 4 — Start TelemetryProcessor

Open a new terminal:

```powershell
dotnet run --project src/EdgePulse.TelemetryProcessor
```

Expected output:
```
info: TelemetryProcessor.Worker[0]
      TelemetryProcessor starting...
info: TelemetryProcessor.Services.ThresholdCacheService[0]
      Loaded N thresholds.
info: TelemetryProcessor.Services.AlertEngineService[0]
      Preloaded N open alerts.
info: TelemetryProcessor.Worker[0]
      TelemetryProcessor ready. Listening on queue 'telemetry.readings'.
```

---

## Step 5 — Start OPC-UA Simulator + Agent

```powershell
docker compose -f infrastructure/docker-compose.onpremise.yml up -d `
    opcua-simulator opcua-agent
```

> **First run:** Docker builds the image (~2 minutes). Subsequent starts are instant.

Wait ~20 seconds for the simulator to start, then watch agent logs:

```powershell
docker logs -f edgepulse-opcua-agent
```

Expected output (repeating every 5 seconds):
```
[Agent] Publishing 20 device readings to RabbitMQ...
[Agent] Published reading for LW_FeedWaterPump
[Agent] Published reading for LW_ContinuousDigester
... (20 devices)
```

In the TelemetryProcessor terminal, you should see messages being consumed:
```
info: Storing telemetry for device 40000001-...
info: Storing telemetry for device 40000002-...
```

---

## Step 6 — Start the Dashboard

Open a new terminal:

```powershell
cd src/EdgePulse.Dashboard
npm install    # first time only
npm run dev
```

Expected:
```
  VITE v6.x.x  ready in 500ms

  ➜  Local:   http://localhost:5173/
  ➜  Network: use --host to expose
```

Open http://localhost:5173 in a browser.

---

## Step 7 — Log In

The Keycloak realm must be configured with the `edgepulse` realm and a test user.

**Quick check — does the realm exist?**
1. Go to http://localhost:8080 → Admin Console (admin/admin)
2. Look for the `edgepulse` realm in the left dropdown.

**If the realm doesn't exist:** Import it:
1. In Keycloak Admin → Realm Settings → Import
2. Import file: `docs/keycloak/edgepulse-realm-export.json` (if present)

**Test user credentials (NordPulp CustomerAdmin):**
- Username: `customeradmin@nordpulp.com` (or as configured in Keycloak)
- Password: `Test@1234`

---

## Step 8 — Verify the Dashboard

After logging in, you should reach `http://localhost:5173/dashboard`.

### KPI Tiles

Within 30 seconds of the OPC-UA Agent starting you should see:

| Tile | Expected value |
|------|----------------|
| Total Devices | 20 |
| Open Alerts | Increases as thresholds are breached |
| Critical Alerts | Increases when CRITICAL thresholds breached |
| Devices at Risk | ≥ 1 after ~2 minutes |

### Alert Trend Chart (7-day bars)

- The last bar (today) increments as new alerts fire
- Previous 6 bars show historical data (zeros if DB was freshly seeded)

### Severity Chart

- Bars for CRITICAL / HIGH / MEDIUM / LOW grow as alerts accumulate

### Top 5 Devices Table

- Populated after at least one device has an active alert
- Ranked by active alert count

---

## Step 9 — Verify Alert Flow (end-to-end)

The OPC-UA simulator fires spikes at random intervals. To trigger an alert immediately:

**Option A: Wait** — spikes fire at ~15% probability per minute per metric.  
Typically 1–3 alerts fire within the first 2 minutes.

**Option B: Force a spike via the Alerts page**
1. Go to `/alerts` in the dashboard
2. If alerts appear: click **Acknowledge** to test state transitions

**Expected flow:**
1. OPC-UA Agent publishes reading with spike value
2. TelemetryProcessor receives it → consecutive counter increases
3. After 3 consecutive breaches → Alert created in SQL Server
4. Dashboard `/alerts` page shows new OPEN alert
5. Dashboard sidebar badge shows count
6. Dashboard KPI tiles update on next 60s poll

---

## Step 10 — Verify Alert Pages

Navigate to `/alerts`:
- Alerts table should show OPEN/ACKNOWLEDGED alerts
- Filter by severity → correctly narrows the list
- Click **Acknowledge** on an open alert → transitions to ACKNOWLEDGED
- Click **Resolve** → transitions to RESOLVED + removed from active counts on dashboard

---

## Troubleshooting

### "Failed to load dashboard data"
- API is not running → start it with `dotnet run --project src/EdgePulse.API`
- Keycloak token expired → hard-refresh the browser; Keycloak will prompt for re-login

### Dashboard redirects to Keycloak login in a loop
- Realm name or client ID mismatch
- Check `src/EdgePulse.Dashboard/src/keycloak.ts`: realm should be `edgepulse`, clientId `edgepulse-dashboard`
- Verify Keycloak client has `http://localhost:5173/*` in Valid Redirect URIs

### No telemetry arriving (TelemetryProcessor idle)
- RabbitMQ queue `telemetry.readings` is empty → check Agent is running
- `docker logs edgepulse-opcua-agent` for errors
- RabbitMQ UI → Queues tab → check message rate on `telemetry.readings`

### OPC-UA Agent: "Connection refused" on startup
- Simulator not started yet — agent retries automatically every 5 seconds
- `docker logs edgepulse-opcua-simulator` to check it started successfully

### SQL Server "Login failed"
- The `--seed` step creates the database; if you skipped it, run it now
- Connection string in `appsettings.json`: `Server=localhost,1433;Database=EdgePulse;User Id=sa;Password=EdgePulse@2026`

### TelemetryProcessor "PRECONDITION_FAILED" on queue declare
- Caused by a stale queue with different arguments in RabbitMQ
- Fix: delete the `telemetry.readings` queue via RabbitMQ UI → Queues tab → Purge / Delete
- Restart TelemetryProcessor

---

## Stopping Everything

```powershell
# Stop local processes: Ctrl+C in each terminal

# Stop Docker services
docker compose -f infrastructure/docker-compose.onpremise.yml down

# Stop and remove volumes (reset all data)
docker compose -f infrastructure/docker-compose.onpremise.yml down -v
```

---

## Quick Reference — Service URLs

| Service | URL | Credentials |
|---------|-----|-------------|
| Dashboard | http://localhost:5173 | Keycloak user |
| API Swagger | http://localhost:5104/swagger | Bearer token |
| API Health | http://localhost:5104/health | None |
| Keycloak Admin | http://localhost:8080 | admin / admin |
| RabbitMQ UI | http://localhost:15672 | edgepulse / EdgePulse@2026 |
| HAProxy Stats | http://localhost:8404/stats | admin / edgepulse123 |
| OPC-UA Server | opc.tcp://localhost:4840 | Anonymous |
