# Sprint 21 — Maintenance Work Orders (#35)

**Date:** July 2026
**Goal:** Close the loop from alert to resolution — auto-created maintenance
work orders with an assignable, guarded lifecycle and per-device history.

---

## What was delivered

### Domain
- `WorkOrder` entity — number (`WO-XXXXXXXX`), title/description, device/mill,
  optional source alert, priority (LOW…CRITICAL), assignee, due date,
  parts/materials, completion audit (by/at/notes).
- **Guarded lifecycle**: `OPEN → INPROGRESS → COMPLETED`,
  `INPROGRESS ↔ ONHOLD`, cancel from any non-terminal state; illegal
  transitions throw (mapped to HTTP 409).

### Alert engine integration (TelemetryProcessor)
- New `WorkOrders` config: `AutoCreateFromAlerts` (default on) +
  `AutoCreateSeverities` (default CRITICAL/HIGH).
- When a qualifying alert fires, `AlertNotifier` opens a work order
  (priority = severity, deduped one-per-alert) and posts a `WORKORDER`
  in-app notification.

### API
- `GET  /api/workorders?status&deviceId&assignedTo` (per-device history via
  `deviceId`; MillManager sees their mill only; Executive read-only)
- `POST /api/workorders` — manual creation
- `POST /api/workorders/{id}/transition` — `start | hold | complete | cancel`
  (+ completion notes and parts); illegal moves → 409
- `PUT  /api/workorders/{id}/assign`

### Dashboard
- **Work Orders page** (`/workorders`, sidebar 🛠️): status filter tabs,
  table with priority/status chips, device context, completion notes inline;
  create modal (device, priority, due date, assignee); complete modal
  (notes + parts used); assign modal; cancel with warning confirm.
  Auto-refresh 60 s. Executive sees a read-only board. en/fi/sv strings.

## Verified end-to-end (live)
1. ✅ 3 breaching vibration readings → `ALERT FIRED [HIGH]` →
   **`WO-6F48F267` auto-created** + `WORKORDER` notification
2. ✅ Illegal `complete` from OPEN → **409**
3. ✅ assign → start → complete (notes + parts) → 204s
4. ✅ Final state: COMPLETED with notes, parts, assignee
5. ✅ 107 unit tests green (6 domain lifecycle + 7 handler tests added)

## Scope notes (honest)
- Parts/materials tracking is a free-text field (`PartsUsed`) — a structured
  parts catalogue/inventory is future work.
- The "calendar" view is the due-date column/sort; a visual calendar is a
  future enhancement.
- Mobile work-order management belongs to the mobile app epic (#31, deferred).
