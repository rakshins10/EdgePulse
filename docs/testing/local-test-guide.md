# EdgePulse — Local End-to-End Test Guide

**Last updated:** 2026-08-18 (v1.0.1) — covers the full v1.0 feature set  
**Target:** Full NordPulp demo — all 20 devices generating telemetry, alerts firing, dashboard live — plus every v1.0 module (Step 11).

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
Vite Dev Server :3000
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
| Dashboard | Local (`npm run dev`) | 3000 |

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

## Step 1b — Configure application secrets (one time)

The committed `appsettings.json` files hold placeholders; the API and
TelemetryProcessor **refuse to start** until real values are supplied (they
print exactly which key is missing). For local dev, store them in
`dotnet user-secrets` — outside the repo, loaded automatically in Development:

```powershell
$API="src/backend/EdgePulse.API/EdgePulse.API.csproj"
$TP="src/backend/EdgePulse.TelemetryProcessor/EdgePulse.TelemetryProcessor.csproj"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=EdgePulse;User Id=sa;Password=EdgePulse@2026;TrustServerCertificate=True;" --project $API
dotnet user-secrets set "ConnectionStrings:MongoDB" "mongodb://edgepulse:EdgePulse%402026@localhost:27017" --project $API
dotnet user-secrets set "Keycloak:ClientSecret" "<Keycloak → Clients → edgepulse-api → Credentials>" --project $API
dotnet user-secrets set "Keycloak:AdminPassword" "admin" --project $API
dotnet user-secrets set "ConnectionStrings:SqlServer" "Server=localhost,1433;Database=EdgePulse;User Id=sa;Password=EdgePulse@2026;TrustServerCertificate=True" --project $TP
dotnet user-secrets set "ConnectionStrings:MongoDB" "mongodb://edgepulse:EdgePulse%402026@localhost:27017" --project $TP
dotnet user-secrets set "ConnectionStrings:RabbitMQ" "amqp://edgepulse:EdgePulse%402026@localhost:5672/edgepulse" --project $TP
```

Details and the Docker/production equivalent (env vars): [`docs/guides/02-configuration-guide.md`](../guides/02-configuration-guide.md) §1a.

## Step 2 — Run EF Migrations + Demo Seed

```powershell
cd C:\Studies\EdgePulse-Application\EdgePulse

# Apply EF Core migrations (creates the EdgePulse database + all tables)
dotnet run --project src/backend/EdgePulse.API -- --seed
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
dotnet run --project src/backend/EdgePulse.API
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
dotnet run --project src/backend/EdgePulse.TelemetryProcessor
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

  ➜  Local:   http://localhost:3000/
  ➜  Network: use --host to expose
```

Open http://localhost:3000 in a browser.

---

## Step 7 — Configure Keycloak (first time only)

### 7a. Realm

1. Go to http://localhost:8080 → Admin Console (admin / admin)
2. Look for the `edgepulse` realm in the left dropdown.
3. **If it doesn't exist:** Realm Settings → Import → `docs/keycloak/edgepulse-realm-export.json` (if present), or create manually.

### 7b. Dashboard client (public)

Create a client for the React app:
- **Client ID:** `edgepulse-dashboard`
- **Client type:** OpenID Connect
- **Client authentication:** OFF (public client)
- **Standard flow:** ON
- **Valid redirect URIs:** `http://localhost:3000/*` AND `http://localhost:3000/`
- **Web origins:** `http://localhost:3000`

### 7c. Protocol mappers (CRITICAL)

The dashboard client must emit custom claims the API reads from the JWT. Without these, the dashboard renders but every API call returns empty (because `TenantId` defaults to `Guid.Empty`).

Clients → `edgepulse-dashboard` → **Client scopes** tab → click `edgepulse-dashboard-dedicated` → **Add mapper → By configuration → User Attribute**, repeat for each:

| Name | User Attribute | Token Claim Name | JSON Type | Multivalued | Add to access token |
|------|---------------|------------------|-----------|-------------|---------------------|
| `tenantId` | `tenantId` | `tenantId` | String | OFF | ON |
| `role` | `role` | `role` | String | OFF | ON |
| `millId` | `millId` | `millId` | String | OFF | ON |
| `areaIds` | `areaIds` | `areaIds` | String | **ON** | ON |

Also add an `email` mapper of type **User Property** (Property = `email`, Claim Name = `email`).

### 7d. Test user

Users → Add User:
- Username: `superadmin`
- Email: any valid email; Email verified: ON

**Credentials** tab → Set password (e.g. `Test@1234`), Temporary: OFF.

**Attributes** tab → add row:
- Key: `tenantId`
- Value: `10000001-0000-0000-0000-000000000001`   ← seeded NordPulp tenant ID

**Role mapping** tab → Assign realm role: `SuperAdmin`.

### 7e. Verify the JWT

Log in to http://localhost:3000. Open DevTools → Network → any `/api/*` request → copy the `Authorization: Bearer ...` token → paste into https://jwt.io.

The decoded **payload** must contain `tenantId`, `role`, `email`. If any are missing, revisit step 7c.

---

## Step 8 — Verify the Dashboard

After logging in, you should reach `http://localhost:3000/dashboard`.

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

## Step 11 — Verify the v1.0 modules

Log in as `superadmin` unless stated. Each row is a real behaviour to observe.

| Module | Do this | Expect |
|--------|---------|--------|
| 🔔 Notifications | Click the bell → click an alert item | Marks read, jumps to Alerts with that row highlighted |
| 📧 Email | Open http://localhost:8025 after an alert fires | An `EdgePulse [HIGH] …` mail |
| 🛠️ Work Orders | Open the auto-created one → Assign → Start → Complete (notes + parts). Try Complete on an OPEN one | Lifecycle advances; the illegal move is refused (409) |
| 📎 Attachments | Devices → any device → upload a PDF → download → delete | Byte-identical download; row disappears |
| 📈 Reports | Change dates; both CSV buttons | Mill table with MTTA/MTTR; CSVs download |
| ⚡ Energy & ESG | Open the page | kWh + CO₂e KPIs, daily bar chart (the two refiners publish `power_consumption`) |
| 🩺 Device Health | Open the page | Worst-first scores; alert-laden pumps rank CRITICAL; click → telemetry |
| 🗺️ Floor Plan | Edit layout → place a device from the tray → drag it | Dot appears; critical devices pulse red |
| 👥 Users | Create a user, change a role, reset password | Rows update; try disabling yourself → refused |
| 📜 Audit Trail | After the steps above | Every action listed with old → new diffs; CSV export |
| 🔗 Integrations | Add a webhook (any Slack/Teams incoming-webhook URL) → Send test | Delivery status shows `200`; message arrives signed |
| ⚙️ Branding | Configuration → Branding → set name + accent → Save | Sidebar name and accent colour change immediately |
| 🌐 Language | Switch to Suomi / Svenska | Whole UI translates |
| 🔐 Roles | Log out; log in as `millmanager` / `operator` / `executive` (all `Test@1234`) | MillManager sees Lakewood only (10 devices); Operator sees their area (4); Executive is read-only and admin pages are hidden |

## Troubleshooting

### "Failed to load dashboard data"
- API is not running → start it with `dotnet run --project src/backend/EdgePulse.API`
- Keycloak token expired → hard-refresh the browser; Keycloak will prompt for re-login

### Dashboard redirects to Keycloak login in a loop
- Realm name or client ID mismatch
- Check `src/EdgePulse.Dashboard/src/keycloak.ts`: realm should be `edgepulse`, clientId `edgepulse-dashboard`
- Verify Keycloak client has `http://localhost:3000/*` in Valid Redirect URIs

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

### Dashboard renders but all KPI tiles show zero
- The JWT is missing the `tenantId` claim → `CurrentUserService.TenantId` returns `Guid.Empty` → all queries scope to a non-existent tenant
- Verify by pasting the JWT at https://jwt.io — the payload MUST contain `tenantId`, `role`, and `email`
- Fix: see **Step 7c** — add the protocol mappers to the `edgepulse-dashboard` client
- After adding mappers, do a full **logout + login** (a refresh reuses the cached token)

### MongoDB telemetry collection stays empty despite TelemetryProcessor running
- Symptom: RabbitMQ shows messages being delivered (`deliver` count rising) but `ack` count stays at 0 and MongoDB has no documents
- Cause: MongoDB.Driver 3.x default `GuidRepresentation` is `Unspecified` → inserting a POCO with `Guid` fields throws `BsonSerializationException` → message is silently nacked
- Fix: `Program.cs` of TelemetryProcessor must register the serializer at startup:
  ```csharp
  BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));
  ```

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
| Dashboard | http://localhost:3000 | Keycloak user |
| API Swagger | http://localhost:5104/swagger | Bearer token |
| API Health | http://localhost:5104/health | None |
| Keycloak Admin | http://localhost:8080 | admin / admin |
| RabbitMQ UI | http://localhost:15672 | edgepulse / EdgePulse@2026 |
| HAProxy Stats | http://localhost:8404/stats | admin / edgepulse123 |
| OPC-UA Server | opc.tcp://localhost:4840 | Anonymous |
