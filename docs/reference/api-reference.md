# REST API Reference

Canonical, always-current reference: **Swagger UI** at
`http://localhost:5104/swagger` (every endpoint, schema and try-it-out).
This page is the map. All endpoints require a Bearer JWT unless noted;
tenant scoping is automatic from claims.

Legend — 👑 SuperAdmin/CustomerAdmin only · 🏭 +MillManager · 🔓 any
authenticated · ✋ Executive read-only applies.

## Configuration (`/api/configuration`)
Lookup CRUD for: `device-types`, `device-statuses`, `maintenance-types`,
`metric-types`, `location-types`, `alert-severities`, `alert-statuses`,
`industry-templates`, plus `lookup-overrides`.
Pattern per type: `GET` 🔓 · `POST`/`PUT {id}`/`DELETE {id}` 🏭 (system
values protected; deletes are deactivations, in-use guarded).

## Localization (`/api/localization`)
`GET locales` 🔓 · locale CRUD 👑 · `GET/PUT translations` (lookup + UI
strings) 👑 · CSV `export`/`import` 👑.

## Organisation (`/api/organisation`)
Mills/areas: `GET` 🔓 · create/update/delete 🏭 (child-guarded).

## Devices (`/api/devices`)
`GET` (filters mill/area) 🔓 · `POST` register → one-time API key 🏭 ·
`PUT {id}` 🏭 · `DELETE {id}` decommission (revokes keys) 🏭.

## Telemetry (`/api/telemetry`)
`GET devices/{deviceId}?from&to&limit` 🔓 — time-series readings (Mongo).

## Alerts (`/api/alerts`)
`GET` (paged, filters) 🔓 · `GET count` 🔓 · `POST {id}/acknowledge` /
`{id}/resolve` ✋ · thresholds: `GET` 🔓, create/update/delete 🏭.

## Notifications (`/api/notifications`)
`GET ?unreadOnly&take` · `GET unread-count` · `POST {id}/read` ·
`POST read-all` — all 🔓 (own tenant).

## Attachments (`/api/attachments`)
`GET ?entityType&entityId` 🔓 · `POST` multipart (≤25 MB, allow-listed
types) 🏭 · `GET {id}/download` 🔓 · `DELETE {id}` 🏭.

## Work orders (`/api/workorders`)
`GET ?status&deviceId&assignedTo` 🔓(✋) · `POST` create 🏭+Operator ·
`POST {id}/transition` (`start|hold|complete|cancel`; 409 on illegal moves) ·
`PUT {id}/assign`.

## Reports (`/api/reports`)
`GET mill-comparison?from&to` (+`/csv`) 🔓 · `GET alerts/csv` 🔓 —
MillManager auto-scoped to their mill.

## Energy / ESG (`/api/energy`)
`GET report?from&to` (+`/csv`) 🔓 — kWh + CO₂e per mill/device + daily.

## Device health (`/api/healthscore`)
`GET devices` 🔓 — 0–100 score, grade, worst metric, days-to-threshold.

## Floor plan (`/api/floorplan`)
`GET {millId}` 🔓 · `PUT devices/{id}/position` 🏭.

## Users (`/api/users`) 👑
`GET` · `POST` (temp password) · `PUT {id}/role` · `PUT {id}/enabled` ·
`POST {id}/reset-password`.

## Audit (`/api/audit`) 👑
`GET ?entityType&action&from&to&take` · `GET csv`.

## Webhooks (`/api/webhooks`) 👑
`GET` · `GET events` · `POST` · `PUT {id}` · `DELETE {id}` ·
`POST {id}/test`.

## Branding (`/api/branding`)
`GET` 🔓 · `PUT` 👑.

## Dashboard (`/api/dashboard`)
`GET summary` 🔓 — role-scoped KPI payload.

## Ingestion service (separate host, `:3000`)
`POST /ingest` — header `X-Device-Key` (no JWT); body `{ metrics: [...] }`.

## Error contract
Problem-details JSON; domain exceptions map to
400 (validation, with `errors` dictionary) · 403 · 404 · 409 (conflict /
illegal lifecycle transition) · 500.
