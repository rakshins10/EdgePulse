# Sprint 27 — 2D Floor Plan (#38)

**Date:** July 2026
**Goal:** The "simpler alternative" mode of the Digital Twin epic — a live 2D
mill map with colour-coded device health and click-through to telemetry.

---

## What was delivered

### Backend
- `Device.FloorX` / `Device.FloorY` — nullable percent coordinates
  (migration `Sprint27_DeviceFloorPosition`), `SetFloorPosition` clamps 0–100.
- `GET /api/floorplan/{millId}` — devices with position, area, live status
  (name + colour) and open/critical alert counts.
- `PUT /api/floorplan/devices/{id}/position` — place or clear (nulls);
  admins + MillManager only (Operator/Executive → 403).

### Dashboard
- **Floor Plan page** (`/floorplan`, sidebar 🗺️): mill selector, grid SVG
  canvas, device dots coloured by live state (critical = red **pulsing**,
  open alert = orange, else status colour), hover tooltips, click → device
  telemetry; 10 s auto-refresh.
- **Edit layout mode** (admins/MillManager): pick an unplaced device from the
  tray → click the map to place it; drag dots to move; double-click removes.
  en/fi/sv strings.

## Verified end-to-end (live)
1. ✅ 10 devices returned per mill; place two → 204, positions round-trip
   with live status + alert counts (`PUMP-LW-001 @ 22.5,35 — 4 open / 2 crit`)
2. ✅ Operator PUT → **403**
3. ✅ 127 unit tests green

## Scope notes (epic #38)
- Shipped: the 2D floor-plan mode (the roadmap's own "simpler alternative"),
  with live health overlay and layout editing.
- 3D model + historical playback remain post-v1.0 showcase items.
