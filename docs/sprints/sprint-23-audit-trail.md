# Sprint 23 — Audit Trail (#36)

**Date:** July 2026
**Goal:** Tamper-evident record of every configuration and data change —
the audit-trail core of the Compliance & Audit epic.

---

## What was delivered

### Backend
- **`AuditLog` entity** — tenant, user, action (CREATED/UPDATED/DELETED),
  entity type/id/display name, property-level `ChangesJson`
  (`{"Prop":{"old":…,"new":…}}`), UTC timestamp. Append-only.
- **Automatic capture in `EdgePulseDbContext.SaveChangesAsync`** — inspects the
  EF change tracker before every save and writes audit rows in the same
  transaction:
  - Added → CREATED; Deleted / soft-delete (`IsDeleted` → true) → DELETED;
    Modified → UPDATED with old→new diffs (noise props `CreatedAt`/`UpdatedAt`
    excluded; no-op modifications skipped)
  - `AuditLog` itself and system `Notification` rows are excluded
  - user resolved FullName → Email → UserId
- `GET /api/audit` (+ `/csv`) — admin-only (SuperAdmin/CustomerAdmin,
  tenant-scoped), filters: entityType, action, from/to, take.
- Migration `Sprint23_AuditLogs` (indexes: tenant+timestamp, entity).

### Dashboard
- **Audit Trail page** (`/audit`, sidebar 📜, admin-only): action + entity
  filters, colour-coded action chips, per-row change list rendered as
  `Prop: old → new` (strikethrough old / green new), CSV export,
  60 s auto-refresh. en/fi/sv strings.

## Verified end-to-end (live)
Create → update → delete of a maintenance type produced:
1. ✅ `CREATED MaintenanceType "Audit Probe"`
2. ✅ `UPDATED` with exact diffs: Color `#3b82f6→#ef4444`, Description
   `∅→changed`, Name `Audit Probe→Audit Probe v2`
3. ✅ Deactivation captured (`IsActive True→False`) — honest: lookup "delete"
   is a deactivation; entities using `IsDeleted` are recorded as DELETED
4. ✅ CSV export with quoted JSON diffs
5. ✅ Operator → **403**
6. ✅ 114 unit tests green (3 new)

## Scope notes
- Writes made by the TelemetryProcessor via raw SQL (alerts, auto work
  orders, notifications) bypass EF and are intentionally outside the audit
  trail — they are machine actions, each already visible in its own table.
- Remaining items of epic #36 (custom report builder, digital signatures,
  scheduled generation) are post-v1.0; ISO-flavoured operational/ESG reports
  shipped in Sprints 19/22.
