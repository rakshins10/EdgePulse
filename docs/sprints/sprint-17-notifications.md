# Sprint 17 — Notifications (Epic #6 completion)

**Date:** July 2026
**Goal:** Close the delivery gap in the alerts pipeline — alerts existed but
nobody was told about them. Adds **in-app notifications** (dashboard bell) and
**email delivery** on every fired alert.

---

## What was delivered

### 1. In-app notification center
- New `Notification` entity (`src/backend/EdgePulse.Domain/Entities/Notification.cs`)
  — tenant-scoped, generic `Type` discriminator (`ALERT` today, extensible),
  optional deep-link (`LinkEntityType`/`LinkEntityId`), read tracking.
- `Notifications` table + covering index `(TenantId, IsRead, CreatedAt)`
  (migration `Sprint17_Notifications`).
- Application handlers: `GetNotificationsQuery`, `GetUnreadNotificationCountQuery`,
  `MarkNotificationReadCommand`, `MarkAllNotificationsReadCommand` — all
  tenant-isolated.
- `NotificationsController`:
  - `GET  /api/notifications?unreadOnly&take`
  - `GET  /api/notifications/unread-count`
  - `POST /api/notifications/{id}/read`
  - `POST /api/notifications/read-all`
- **Dashboard bell** (`components/layout/NotificationBell.tsx`) in the topbar:
  unread badge (polls every 30 s), dropdown panel with severity dot, relative
  timestamps in the active language, mark-read on click, "mark all read",
  deep-link to the Alerts page. en/fi/sv strings included.

### 2. Email delivery
- `AlertNotifier` in the TelemetryProcessor — called by `AlertEngineService`
  right after an alert row is inserted. Both channels are best-effort (failures
  logged, never block alert creation):
  1. inserts the in-app `Notifications` row (raw SQL, consistent with the worker),
  2. sends a plain-text summary email via SMTP (MailKit 4.9.0).
- Config section `Smtp` in TelemetryProcessor `appsettings.json`
  (`Enabled/Host/Port/UseSsl/User/Password/From/Recipients[]`).
- **MailHog** added to `docker-compose.onpremise.yml` — local SMTP catcher.
  All alert emails land at **http://localhost:8025**; nothing leaves the machine.
  Production points the same config at a real SMTP relay.

## Design decisions
- **Polling, not WebSocket** — the dashboard already polls with React Query;
  a 30 s badge refresh is adequate for alert traffic and needs no new infra.
- **Raw SQL in the worker** — the TelemetryProcessor deliberately avoids EF;
  the notifier follows its existing pattern.
- **Recipients in config, not per-tenant UI (yet)** — a `Smtp:Recipients` array
  covers the single-tenant demo honestly; per-tenant recipient management is a
  listed follow-up.
- Device labels are cached in-memory (`DeviceId → "Name (CODE)"`) to keep
  notification text friendly without a query per metric.

## Verified end-to-end (live)
Published 3 breaching `bearing_temp` readings (92.5 > 75, ConsecutiveCount 3)
straight to RabbitMQ:
1. ✅ `ALERT FIRED [HIGH]` — alert row created
2. ✅ `Notifications` row created (in-app)
3. ✅ Email captured by MailHog (`EdgePulse [HIGH] Feed Water Pump …`)
4. ✅ API: unread-count 1 → mark-read 204 → unread-count 0
5. ✅ 70 unit tests green (7 new notification handler tests)

## Files (key)
| File | Change |
|------|--------|
| `Domain/Entities/Notification.cs` | new entity |
| `Infrastructure/Persistence/Configurations/NotificationConfiguration.cs` | new |
| `Infrastructure/Migrations/*Sprint17_Notifications*` | new table + index |
| `Application/Features/Notifications/**` | 2 queries + 2 commands |
| `API/Controllers/NotificationsController.cs` | new |
| `TelemetryProcessor/Services/AlertNotifier.cs` | new (in-app + email fan-out) |
| `TelemetryProcessor/Program.cs`, `appsettings.json` | wiring + `Smtp` config |
| `infrastructure/docker-compose.onpremise.yml` | MailHog service |
| `Dashboard src/components/layout/NotificationBell.*` | new bell + panel |
| `Dashboard src/api/notifications.ts`, `types/api.ts`, locales | client + i18n |

## Follow-ups
- Per-tenant notification settings (recipients, channel toggles) as UI config
- Notification triggers for future modules (work orders — Sprint 21)
