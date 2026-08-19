# EdgePulse — Functionality Guide

What the application does, module by module, and who can do what.

EdgePulse is a multi-tenant Industrial IoT platform: it ingests live
telemetry from factory devices, watches it against configurable thresholds,
tells the right people when something is wrong, opens the maintenance work
that fixes it, and reports on operations, energy and compliance.

---

## 1. Roles

| Role | Scope | Can |
|------|-------|-----|
| **SuperAdmin** | Platform | Everything, across tenants |
| **CustomerAdmin** | Their tenant | Everything in-tenant incl. users, webhooks, branding |
| **MillManager** | Their mill | Operate + configure their mill (thresholds, layout, work orders, uploads) |
| **Operator** | Assigned areas | View, acknowledge/resolve alerts, work work orders |
| **Executive** | Tenant (read-only) | Dashboards, reports, health — no changes |

Hierarchy: **Platform → Tenant → Mill → Area → Device**.

## 2. Modules

### 📊 Dashboard (`/dashboard`)
Role-scoped KPI overview: device counts, open/critical alerts, 7-day alert
trend, severity split, top offending devices. Auto-refreshes.

### 🔌 Devices (`/devices`)
Register devices (issues a one-time **API key**), edit, decommission
(revokes keys, preserves telemetry). Device detail = live Recharts
telemetry per metric with global/per-chart time ranges + **file
attachments** (manuals, datasheets, CAD; 25 MB, role-gated). The header
carries a **✦ Ask about this device** link (Sprint 30) that opens Ask
EdgePulse focused on that device.

### 🔔 Alerts (`/alerts`)
The alert engine (Telemetry Processor) fires when a metric breaches its
threshold for N consecutive readings (default 3 — filters sensor noise).
Lifecycle: OPEN → ACKNOWLEDGED → RESOLVED with audit fields. Deduplication:
one open alert per (device, metric, threshold).

**Delivery** (Sprint 17): every alert becomes an in-app notification
(topbar 🔔 bell — badge, mark-read, deep link) **and** an SMTP email; plus
signed **webhooks** (see Integrations).

**✦ Explain** (Sprint 29): every alert row has an **✦ Explain** button
(shown only when the API reports AI as enabled via `/api/ai/status`). It
opens a panel with three sections written by a local LLM — **WHAT
HAPPENED / LIKELY CAUSES / RECOMMENDED ACTION** — generated on demand and
cached on the alert (a "cached" note shows when served from cache;
**Regenerate** bypasses the cache; **Retry** when the model is
unavailable). A disclaimer reminds that it is a structured starting point,
not a diagnosis. Available to every role that can view alerts. Full detail:
[AI guide](05-ai-guide.md).

### ✦ Ask EdgePulse (`/ask`)
Natural-language questions answered from **live** data (Sprint 30): a
chat-style thread with example prompts (Enter to send). Ask "which devices
have the most alerts this week?", "what is wrong with MOTOR-LW-002?" or
"any open work orders?" — the API gathers only the devices, alerts (last 30
days + still open) and open work orders the caller is allowed to see
(tenant + role scoping), hands that data to the model and instructs it to
answer **only** from it, cite devices by name + code and say when something
is missing. Each answer shows a **Grounded on: …** line (which devices /
how many alerts and work orders were in scope) and a disclaimer naming the
provider. Arriving from a device page shows a clearable **Focused on
<device>** chip; mentioning device codes/names in the question focuses on
those (max 3); otherwise a plant-wide snapshot is used. Nothing is cached
or written. Available to every role; when `Ai:Provider=none` the page
explains that AI is disabled. Full detail: [AI guide](05-ai-guide.md).

### 🛠️ Work Orders (`/workorders`)
CRITICAL/HIGH alerts auto-open a work order (config-gated). Guarded
lifecycle OPEN → INPROGRESS → COMPLETED (hold/cancel), assignment,
completion notes + parts used. Per-device maintenance history via the
device filter. Executives read-only.

### 🗺️ Floor Plan (`/floorplan`)
Live 2D mill map: device dots coloured by state (pulsing red = critical
alert, orange = open alert, else status colour), hover details, click →
telemetry. Admins/MillManagers edit the layout (place from tray, drag,
double-click to remove).

### 🩺 Device Health (`/health`)
Transparent statistical condition score per device (100 minus penalties for
open alerts, threshold proximity and 7-day degradation trend) with an
estimated days-to-threshold (linear). Worst first; click through to
telemetry. An indicator, not a guarantee — the methodology is shown on-page.

### ⚡ Energy & ESG (`/energy`)
Daily energy (kWh) aggregated from power telemetry, CO₂e via a configurable
grid factor, per-mill/per-device breakdowns, daily chart, ESG CSV export
(GHG Protocol Scope 2, location-based).

### 📈 Reports (`/reports`)
Cross-mill comparison for any date range: devices, alert volumes,
severities, **MTTA/MTTR**. CSV exports (comparison + full alert detail).

### 🏭 Mills & Areas (`/mills`, `/areas`)
Organisation management with location types, deployment mode and timezone
per mill. Deletes are guarded when children exist.

### ⚙️ Configuration (`/configuration`)
Everything-is-data: lookup types (device types, statuses, maintenance
types, metric types, location types) with tenant custom values and
protected system values; **Languages & Translations** (en/fi/sv shipped,
add any locale, DB-backed UI strings, CSV translation round-trip);
**Branding** (white-label name/logo/accent).

### 👥 Users (`/users`, admins)
Full user administration against Keycloak: create with temporary password,
role + mill/area scoping, enable/disable, password reset. CustomerAdmins
cannot touch other tenants or mint SuperAdmins; nobody can disable
themselves.

### 📜 Audit Trail (`/audit`, admins)
Every create/update/delete captured automatically with property-level
old → new diffs, filterable, CSV evidence export.

### 🔗 Integrations (`/integrations`, admins)
Outbound webhooks for `alert.created` / `workorder.created`:
HMAC-SHA256-signed JSON, or Slack/Teams-compatible text format. Test-fire
button + delivery status per subscription.

## 3. Data ingestion paths

1. **OPC-UA Edge Agent** (primary on-prem path) — subscribes to OPC-UA
   variables and publishes to RabbitMQ. `npm run discover` browses a server
   and generates the device/metric mapping.
2. **REST Ingestion** — `POST /ingest` with the device API key
   (`X-Device-Key`) for anything that can send JSON.

Both feed: RabbitMQ → Telemetry Processor → MongoDB (time-series) + alert
engine → SQL (alerts) → notifications/emails/webhooks/work orders.

## 4. Localization

Full UI in **English, Finnish, Swedish**; locales are data (add German in
Configuration → Languages, optionally pre-filled from English, then
translate in-app or round-trip a CSV). Lookup names resolve server-side via
`Accept-Language`.
