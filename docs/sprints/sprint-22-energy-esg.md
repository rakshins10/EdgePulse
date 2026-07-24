# Sprint 22 — Energy Monitoring & ESG Reporting (#33)

**Date:** July 2026
**Goal:** Turn the power telemetry that devices already publish into energy
(kWh) and CO₂-equivalent figures with an ESG report page and CSV export.

---

## What was delivered

### Backend
- **`EnergyController`** — follows the TelemetryController precedent (time-series
  data is queried in Mongo directly, never via EF/SQL):
  - `GET /api/energy/report?from&to` — totals, per-mill and per-device energy
    + CO₂, and a daily series
  - `GET /api/energy/report/csv` — per-device ESG export
- **Mongo aggregation pipeline** does the heavy lifting server-side:
  match (tenant, range, power metric keys) → unwind → group by
  device × day (avg power, first/last timestamp, sample count). Only the
  small grouped result crosses the wire (~450k raw readings stay in Mongo).
- **`EnergyMath`** (Application, pure/testable): energy ≈ average measured
  power × observed duration per daily bucket; CO₂e = kWh × grid factor.
- Config (`Esg` section):
  - `PowerMetricKeys` — metric keys treated as instantaneous kW
    (default `power_consumption`; extendable per deployment)
  - `Co2FactorKgPerKwh` — grid carbon intensity (default 0.181, ~EU-27
    average; set per country — e.g. Finland ≈ 0.07)

### Dashboard
- **Energy & ESG page** (`/energy`, sidebar ⚡): date range, KPI tiles
  (energy kWh, CO₂e kg with the factor shown, metered device count), daily
  kWh bar chart (Recharts, theme-aware), per-device breakdown table with a
  methodology note (GHG Protocol Scope 2, location-based), ESG CSV download.
  en/fi/sv strings.

## Verified end-to-end (live, real telemetry)
- 2 metered devices (Primary Refiners, ~1.85 MW and ~1.65 MW average)
- ✅ Report: **137 997.7 kWh**, **24 977.6 kg CO₂e** @ 0.181, per-mill rollup
  (Lakewood 72 957.5 / Riverside 65 040.2 kWh), 6 daily points
- ✅ CSV export with BOM + correct quoting
- ✅ 111 unit tests green (4 new EnergyMath tests)

## Honest scope notes
- Energy is *approximated* from instantaneous power samples (avg × observed
  duration per day). Utility-grade metering would use cumulative kWh counters —
  the module can adopt those as a future metric type without API changes.
- EU-taxonomy alignment reporting and scheduled report delivery are follow-ups
  (delivery can reuse the Smtp config from Sprint 17).
