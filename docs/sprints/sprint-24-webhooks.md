# Sprint 24 — Webhooks & Integrations (#40)

**Date:** July 2026
**Goal:** Push EdgePulse events into external systems (Slack, Teams, custom
services) — the integration-hub core of Epic 20.

---

## What was delivered

### Backend
- **`WebhookSubscription` entity** — name, URL, signing secret, subscribed
  events (CSV), format (`json`/`slack`), active flag, last delivery status.
- **HMAC-SHA256 signing** (`WebhookSigner`, header `X-EdgePulse-Signature`,
  hex digest of the raw body) so receivers can authenticate deliveries;
  `X-EdgePulse-Event` carries the event key.
- **Two dispatch paths** (same wire format):
  - API `WebhookSender` (typed HttpClient, 10 s timeout) — used by the
    admin **test-fire** endpoint
  - TelemetryProcessor `WebhookDispatcher` (dependency-free, raw SQL +
    HttpClient) — fires **`alert.created`** on every alert and
    **`workorder.created`** on auto-created work orders; records delivery
    status per subscription
- **Slack format**: `{"text": ":zap: EdgePulse `event` — …"}` — drop a Slack
  or Teams incoming-webhook URL in and it just works.
- `WebhooksController`: list / events / create / update (empty secret keeps
  existing) / delete / **test** — admin only. Migration `Sprint24_Webhooks`.

### Dashboard
- **Integrations page** (`/integrations`, sidebar 🔗, admin-only): table with
  event chips, format, last delivery status (+timestamp), Send-test / edit /
  delete; add/edit modal with event checkboxes and format select.
  en/fi/sv strings.

## Verified end-to-end (live, local signed receiver)
1. ✅ Create subscription → test-fire → receiver: `sigValid: true`
2. ✅ Real alert (3 breaching readings) → **`alert.created` delivered,
   signature valid** → auto work order → **`workorder.created` delivered,
   signature valid**
3. ✅ TP logged deliveries + recorded status `200` on the subscription
4. ✅ 121 unit tests green (7 new: signer, entity matching, CRUD, test-fire,
   tenant isolation)

## Scope notes (epic #40)
- Shipped: public REST API (Swagger since Sprint 1), signed webhooks for
  engine events, Slack/Teams-compatible format, no-code subscription UI.
- Pre-built SAP/ServiceNow/PagerDuty connectors and an API marketplace remain
  post-v1.0 — webhooks are the foundation they build on.
