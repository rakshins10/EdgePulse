# Sprint 19 — Reports & Exports (Epic #7 completion)

**Date:** July 2026
**Goal:** Close the "Reports" half of Epic 7 — cross-mill operational comparison
and CSV exports, with a dedicated Reports page.

---

## What was delivered

### Backend
- `GetMillComparisonReportQuery(from, to)` — per mill over a date range:
  device count, alerts triggered, currently-open alerts, critical/high split,
  **MTTA** (avg minutes to acknowledge) and **MTTR** (avg minutes to resolve).
  Role-scoped: MillManager sees only their mill. Aggregation happens in memory
  after a narrow projection — mill counts are tiny and it keeps the query
  provider-agnostic (unit-testable on EF InMemory).
- `ExportAlertsCsvQuery(from, to)` — full alert detail export (device, metric,
  values, severity, status, ack/resolve audit fields).
- **`CsvBuilder`** (Application/Common) — minimal RFC-4180 writer (quoting,
  doubled quotes, invariant numbers); controllers add a UTF-8 BOM so Excel
  opens files correctly.
- `ReportsController`:
  - `GET /api/reports/mill-comparison?from&to` (JSON; defaults last 30 days)
  - `GET /api/reports/mill-comparison/csv`
  - `GET /api/reports/alerts/csv`

### Dashboard
- **Reports page** (`/reports`, sidebar 📊): date-range picker, comparison
  table (critical/high colour-coded, tabular numerals), two CSV download
  buttons (uses the Content-Disposition filename). en/fi/sv strings.

### Hardening (found during verification)
- `EnableRetryOnFailure(3, 5s)` added to `UseSqlServer` — a Docker Desktop
  port-relay hiccup surfaced transient pre-login handshake failures as 500s;
  EF now retries them.

## Verified end-to-end (live)
- JSON report: 2 mills (Lakewood/Riverside) with real device + alert figures
- Comparison CSV: correct quoting (`"Lakewood, Finland"`), BOM present
- Alerts CSV: real alert rows incl. the Sprint 17 test alert
- 84 unit tests green (5 new report tests incl. MillManager scoping and
  CSV comma-escaping)

## Notes / lessons
- The whole Docker stack silently restarted mid-session; Keycloak, SQL and
  RabbitMQ host-port relays came back wedged (HTTP 000 / handshake resets)
  until each container was restarted. The EF retry policy now absorbs the
  transient window; a `docker restart <container>` clears a wedged relay.

## Follow-ups
- Scheduled report generation + email delivery (ties into Smtp config)
- Energy/ESG report lands in Sprint 22
