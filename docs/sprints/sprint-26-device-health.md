# Sprint 26 — Device Health Scoring (#32)

**Date:** July 2026
**Goal:** The statistical core of predictive maintenance — a transparent
0–100 condition score per device with a naive linear days-to-threshold
estimate. Deliberately explainable arithmetic, not opaque ML.

---

## What was delivered

### Backend
- **`HealthMath`** (Application, pure/tested):
  - alert penalty (severity-weighted, capped at 60)
  - threshold utilization (recent avg vs alert limit) + banded penalty
  - least-squares slope of 7-day daily averages (≥3 points or no claim)
  - days-to-threshold = linear extrapolation (only when degrading, ≤90 d)
  - score = 100 − penalties → grade GOOD / WATCH / DEGRADED / CRITICAL
- **`HealthScoreController`** (`/api/healthscore/devices` — `/health` stays
  the liveness probe): Mongo pipeline groups 7 days of telemetry into daily
  averages per device×metric; joined with SQL thresholds + open alerts;
  every thresholded metric evaluated, the worst one reported.

### Dashboard
- **Device Health page** (`/health`, sidebar 🩺): worst-first table with
  score bar (grade-coloured), condition chip, open alerts, "metric to watch"
  (recent avg + % of limit), estimated days to threshold (highlighted ≤30/≤7),
  row click → device telemetry. Honest methodology note. en/fi/sv strings.

## Verified end-to-end (live, real fleet)
20 devices scored; ranking matches reality:
```
 28 CRITICAL  Feed Water Pump (LW)  4 open alerts, bearing_temp @ 83.7 %
 30 CRITICAL  Continuous Digester   2 alerts, temperature @ 90 %
 48 DEGRADED  Feed Water Pump (RV)  2 alerts, bearing_temp @ 81.6 %
 88 GOOD      Black Liquor Pump     0 alerts
```
Slopes are ~0/day on the stationary simulator → days-to-threshold honestly
reports "stable". 127 unit tests green (6 new HealthMath tests).

## Scope notes (epic #32)
- Shipped: transparent statistical scoring + linear RUL indicator.
- Real ML (trained RUL models, Azure ML / ONNX inference) remains post-v1.0 —
  this module defines the data plumbing and UI those models would feed.
