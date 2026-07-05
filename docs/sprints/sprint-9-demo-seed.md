# Sprint 9 — Demo Data Seed

**Branch:** `feature/sprint-9-demo-seed`
**Merged:** 2026-05-29
**Stories:** #69, #70, #71
**Status:** ✅ Complete

---

## Goal

Provide a fully deterministic, idempotent demo seed that populates the NordPulp Industries
scenario with fixed GUIDs so that all demo curl scripts and documentation stay permanently
valid. Also fix the TelemetryProcessor restart gap where alert deduplication was cleared
each time the processor restarted.

---

## What Was Built

### 1. `DemoIds.cs` — Fixed GUIDs for All Demo Entities

`src/backend/EdgePulse.Domain/Constants/DemoIds.cs`

All demo entities have permanent, hard-coded GUIDs with a recognisable prefix pattern:

| Prefix | Entity |
|--------|--------|
| `10000001-…` | Tenant |
| `20000001-…` | Mills |
| `30000001-…` | Lakewood Areas |
| `30000002-…` | Riverside Areas |
| `40000001-…` | Lakewood Devices |
| `40000002-…` | Riverside Devices |
| `50000001-…` | Lakewood Thresholds |
| `50000002-…` | Riverside Thresholds |

Purpose: documentation, curl scripts, and integration tests reference the same IDs forever.

---

### 2. `DemoSeedService` — Idempotent Seed

`src/backend/EdgePulse.Infrastructure/Persistence/Seeding/DemoSeedService.cs`

**Seeded entities:**
- 1 Tenant: NordPulp Industries (`nordpulp`)
- 2 Mills: Lakewood Mill (Finland) + Riverside Mill (Sweden)
- 8 Areas: 4 per mill (Pulp Line A, Paper Machine 1, Recovery Boiler, Utilities)
- 20 Devices: 10 per mill using `PulpAndPaperDeviceTypeIds` constants
- 21 Alert Thresholds: real-world Pulp & Paper values across Critical/High/Medium severity

**Idempotency strategy:**
1. Check by demo ID — if present, skip
2. If ID not found, check by slug (`nordpulp`) — if that exists with a *different* ID (e.g. from a prior manual session), cascade-delete the entire tenant subtree then re-insert with the demo ID
3. All downstream entities (mills, areas, devices, thresholds) follow the same check-by-ID pattern

**SeedDate anchor:** All `CreatedAt`/`UpdatedAt` timestamps are anchored to
`2026-01-01T00:00:00Z` so the database looks like a production environment
from day one of the year, not today.

---

### 3. `--seed` CLI Flag in Program.cs

`src/backend/EdgePulse.API/Program.cs`

```bash
dotnet run --project src/backend/EdgePulse.API --seed
```

- Resolves `EdgePulseDbContext` and `ILogger<DemoSeedService>` from the DI container
- Runs `DemoSeedService.SeedAsync()`
- Prints "Demo seed complete. Exiting." and returns without starting Kestrel
- Safe to run multiple times (fully idempotent)

---

### 4. `PreloadOpenAlertsAsync` — Alert Dedup Fix

`src/backend/EdgePulse.TelemetryProcessor/Services/AlertEngineService.cs`

**Problem:** The `_openAlertKeys` deduplication dictionary lived only in memory. Every
processor restart cleared it, causing any OPEN/ACKNOWLEDGED alert to be re-fired on the
next threshold breach.

**Fix:** `PreloadOpenAlertsAsync(ct)` runs once on startup:

```csharp
SELECT DeviceId, MetricKey, AlertThresholdId
FROM   Alerts
WHERE  StatusCode IN ('OPEN', 'ACKNOWLEDGED')
```

Populates `_openAlertKeys` so the processor's dedup state survives restarts.

**Worker startup sequence (updated):**

```
1. ThresholdCacheService.RefreshAsync()      ← load threshold rules
2. AlertEngineService.PreloadOpenAlertsAsync() ← NEW: restore dedup state
3. ConnectRabbitMqAsync()                    ← start consuming
```

---

## Demo Thresholds — Real-World Values

| Device | Metric | Severity | Condition |
|--------|--------|----------|-----------|
| Pulp Pump | vibration_rms | CRITICAL | > 12.0 mm/s |
| Pulp Pump | bearing_temp | HIGH | > 85°C |
| Pulp Pump | flow_rate | HIGH | < 50 L/min |
| Paper Machine | nip_pressure | CRITICAL | < 180 kN/m |
| Paper Machine | wire_speed | HIGH | > 950 m/min |
| Recovery Boiler | flue_gas_temp | CRITICAL | > 420°C |
| Recovery Boiler | steam_pressure | HIGH | > 9.2 MPa |
| Compressor | discharge_pressure | HIGH | > 8.8 bar |
| Compressor | motor_current | MEDIUM | > 48 A |
| Compressor | vibration_rms | HIGH | > 8.5 mm/s |
| (+ 11 more across Riverside Mill) | | | |

---

## Files Changed

| File | Change |
|------|--------|
| `src/backend/EdgePulse.Domain/Constants/DemoIds.cs` | **NEW** — all demo fixed GUIDs |
| `src/backend/EdgePulse.Infrastructure/Persistence/Seeding/DemoSeedService.cs` | **NEW** — idempotent seed |
| `src/backend/EdgePulse.API/Program.cs` | Modified — `--seed` CLI flag |
| `src/backend/EdgePulse.TelemetryProcessor/Services/AlertEngineService.cs` | Modified — `PreloadOpenAlertsAsync` |
| `src/backend/EdgePulse.TelemetryProcessor/Worker.cs` | Modified — call preload on startup |
| `docs/domain/02-demo-data-setup.md` | **NEW** — full demo reference doc |

---

## How to Run the Seed

```bash
# Ensure docker is running (SQL Server needed)
cd c:/Studies/EdgePulse-Application/EdgePulse
docker compose -f infrastructure/docker-compose.onpremise.yml up -d sqlserver

# Apply latest migrations first (if needed)
dotnet ef database update \
  --project src/backend/EdgePulse.Infrastructure \
  --startup-project src/backend/EdgePulse.API

# Run seed
dotnet run --project src/backend/EdgePulse.API --seed
```

Expected output:

```
info: DemoSeedService: Starting demo data seed...
info: DemoSeedService: Tenant NordPulp already exists with correct ID — skipping.
info: DemoSeedService: Mill Lakewood Mill already exists — skipping.
...
info: DemoSeedService: Demo seed complete.
Demo seed complete. Exiting.
```

---

## Error Fixes in This Sprint

| Error | Root Cause | Fix |
|-------|-----------|-----|
| `IX_Tenants_Slug` duplicate key | Existing nordpulp tenant had a different ID from a prior manual session | Added slug-based check: if wrong ID exists, cascade-delete then re-insert |
| `ExecuteSqlRawAsync` CancellationToken param error | `ct` passed as a positional SQL parameter instead of named arg | Changed to `parameters: new object[] { id }, cancellationToken: ct` |

---

## Documentation Added

- `docs/domain/02-demo-data-setup.md` — Complete demo reference:
  - All fixed GUIDs for every entity
  - 5 end-to-end alert demo scenarios with curl commands
  - Prerequisites and reset instructions
  - Threshold values rationale table (industry standards)

---

## Known Limitations

- Demo seed does not create Keycloak users — those must be created manually via
  `docs/keycloak-setup.md` or imported via realm JSON.
- Telemetry readings (MongoDB) are not seeded — use the Ingestion API or simulator
  to generate live readings.
- Alert records are not pre-seeded — they are created by the TelemetryProcessor
  when breaches occur.

---

## Next: Sprint 10 — Dark Mode + Responsive Layout

- Toggle between dark and light themes
- Responsive breakpoints for tablet and mobile
- CSS custom properties for theme variables (no Tailwind, no CSS library)
