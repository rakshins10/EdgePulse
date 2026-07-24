# Changelog — Dashboard

All notable changes to `edgepulse-dashboard` are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/);
versions follow [SemVer](https://semver.org/).

> **How versioning works.** The version on the **`## [Unreleased]`** line below is
> the *next* version. Every merge to `main` that touches the dashboard publishes a
> beta image tagged `<next>-beta.<N>` (N auto-increments per merge). Cutting a
> release — pushing a `dashboard-v<next>` tag — publishes the stable image, then you
> rename the section below to `## [<next>] — YYYY-MM-DD` and start a fresh
> `## [Unreleased]` with the next target.

## [Unreleased] — v0.1.0

First release line. Everything built across Sprints 8–16 ships here.

### Added
- **Device Health page (Sprint 26)** — worst-first condition board with
  score bars, grades and days-to-threshold indicators.
- **Integrations page (Sprint 24)** — admin webhook management with event
  chips, delivery status and send-test.
- **Audit Trail page (Sprint 23)** — admin-only change log with action/entity
  filters, old→new diff rendering and CSV export.
- **Energy & ESG page (Sprint 22)** — KPI tiles, daily kWh bar chart,
  per-device breakdown, ESG CSV download.
- **Work Orders page (Sprint 21)** — status-filtered board with priority/
  status chips, create/complete/assign modals, guarded transitions.
- **Users page (Sprint 20)** — admin-only user administration: create with
  temporary password, role chips, change-role / reset-password modals,
  enable/disable.
- **Reports page (Sprint 19)** — date-range cross-mill comparison table with
  CSV downloads.
- **Attachments card (Sprint 18)** — upload/download/delete files on the
  device detail page (hidden for read-only roles).
- **Notification bell (Sprint 17)** — topbar bell with unread badge (30 s poll),
  dropdown panel with severity dots and relative timestamps, mark-read /
  mark-all-read, deep-link to Alerts. en/fi/sv strings.
- Designed confirm dialog + toast notification system replacing native
  `confirm()`/`alert()` across the app.
- React + Vite single-page dashboard served via nginx.
- **Alerts UI** wired to the alerts engine.
- **Dark mode** with a responsive layout.
- **Executive Dashboard** — KPI overview for the Executive role.
- **Devices / Mills / Areas** pages with **live telemetry charts**.
- **Configuration page** for managing lookup values.
- **Create / Decommission** device flows.
- **Mill / Area / Device Edit + Delete** UI, covered by end-to-end tests.
- **Internationalization**: i18n infrastructure with English, Finnish, and Swedish
  translations; DB-backed UI strings; CSV translation round-trip.

### Changed
- UX polish — collapsible sidebar, viewport-aware scrolling, refined telemetry
  layout.
- Dockerized (nginx) with `/api` proxy and SPA fallback; CI/CD publishing to GHCR.

### Fixed
- Aligned API ports and health checks with the backend during local run-up.
