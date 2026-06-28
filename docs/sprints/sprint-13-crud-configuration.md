# Sprint 13 — Devices/Mills/Areas Pages + Configuration CRUD

**Status:** Done
**Commits:** `55c79c1` (page restoration), `99588cf` (Create + Configuration)
**Date completed:** 2026-05-31

---

## Goal

Restore the management UIs that were dropped when routing was rebuilt around the
Alerts engine in Sprint 8, then add Create operations and a Configuration
(lookup-table) management screen. The backend APIs already existed; this sprint
was primarily frontend.

---

## Background

A history check (`git log`) showed the Devices, Device Detail, and Organisation
pages — plus a Recharts telemetry chart with a time-scale toolbar — existed in
earlier commits (`19f6508`, `fa198e5`, `4ff1075`) but were replaced with
placeholder pages during the Sprint 8 routing refactor. So step one was
restoration, not green-field work.

## What was built

### Restored pages (commit `55c79c1`)
- **DevicesPage** — table with mill/area filters, status badges, row → detail
- **DeviceDetailPage** — breadcrumb, info grid, per-metric **Recharts** area
  charts, global + per-chart time-range toolbar (`useTimeRange` hook:
  day/week/month/year/custom)
- **MillsPage** / **AreasPage** — mills as cards with their areas; areas table
- Restored API clients (`devices`, `organisation`, `telemetry`) + the
  `TelemetryController` (`GET /api/telemetry/devices/{id}`) lost in the refactor,
  including the MongoDB GuidSerializer registration

### Create + Configuration (commit `99588cf`)
- **Register Device** modal (with one-time API-key reveal) + **Decommission**
- **Add Mill** and **Add Area** modals
- **Configuration page** — tabbed CRUD for the lookup tables (Location Types,
  Device Types, Device Statuses, Maintenance Types, Metric Types)
- Reusable `Modal` + `FormField` components, all plain CSS Modules

---

## Design decisions

| Decision | Rationale |
|----------|-----------|
| Restore from git, not rewrite | The old pages were complete and good; recovering them was faster and preserved prior design intent. |
| Recharts (not pure SVG) for device telemetry | The per-metric detail charts need zoom/scale interactions; Recharts was already the chosen lib in the restored code. (The executive dashboard keeps its dependency-free SVG charts.) |
| Configuration as tabs | One screen for all lookup types keeps the nav simple. |

---

## Notes / deferred

- Edit and Delete for Mills/Areas/Devices were **not** included here (no backend
  PUT/DELETE endpoints existed yet) — became Sprint 14.
- The "Floor" concept users asked about is a **LocationType value**, not a new
  hierarchy level; managed under Configuration → Location Types.
