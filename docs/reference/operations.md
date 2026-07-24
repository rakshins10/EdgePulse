# Operations Guide

Monitoring, backup, security hardening, upgrade and troubleshooting.

## 1. Monitoring

| Signal | Where |
|--------|-------|
| API liveness | `GET /health` → `{"status":"healthy"}` |
| Processor liveness | log line `TelemetryProcessor ready…`; RabbitMQ UI (15672) consumer on `telemetry.readings` |
| Queue depth | RabbitMQ UI — a growing `telemetry.readings` means the processor is down/slow |
| Alert flow | 🔔 bell / Alerts page; MailHog (dev) or the mail relay |
| Webhook deliveries | 🔗 Integrations — last status + timestamp per subscription |
| Business health | 🩺 Device Health, 📈 Reports (MTTA/MTTR) |
| HAProxy | stats UI :8404 |

Log destinations are console (container logs); ship them with your
platform's collector (Loki/App Insights per deployment profile).

## 2. Backup

| Store | What to back up | How |
|-------|----------------|-----|
| SQL Server | Everything relational (config, alerts, work orders, audit…) | Native `BACKUP DATABASE EdgePulse` or volume snapshots |
| MongoDB | `edgepulse_telemetry` | `mongodump` on schedule; telemetry is append-only — incremental friendly |
| Keycloak (Postgres) | Realm, users, federation config | `pg_dump keycloak` |
| Attachments | `Storage:AttachmentsRoot` volume | File-level backup |

Restore order: databases → start API (migrations no-op) → services.

## 3. Security hardening checklist

- [ ] Change every default credential in the compose file (SQL sa, Mongo,
      RabbitMQ, Keycloak admin, client secret).
- [ ] Keycloak in production mode behind HTTPS; disable `start-dev`.
- [ ] Replace the master-admin user-management credentials with a
      `manage-users` service account (see authentication guide).
- [ ] TLS at HAProxy; dashboard and API only reachable through it.
- [ ] Restrict RabbitMQ/Mongo/SQL to the internal network (no host ports in
      production compose).
- [ ] Rotate device API keys on personnel change (decommission + re-register
      or key revoke endpoint).
- [ ] Webhook secrets ≥ 16 chars; receivers must verify
      `X-EdgePulse-Signature`.
- [ ] Review the 📜 Audit Trail regularly; export CSV for evidence packs.

## 4. Upgrade procedure

1. Read the component `CHANGELOG.md`s for the target versions.
2. Back up (section 2).
3. `docker compose pull` the new image tags (pin exact `X.Y.Z`).
4. Apply DB migrations: run the new API image once with
   `dotnet ef database update` (or from a workstation against the DB).
5. Restart services in order: processor → API → dashboard.
6. Smoke-check: `/health`, one live chart, fire a test webhook.

Rollback: images are immutable per version — repoint tags and restore the
DB backup if a migration must be reverted.

## 5. Troubleshooting

| Symptom | Likely cause | Fix |
|---------|-------------|-----|
| HTTP 000 / connection refused to a container port | Docker Desktop port-relay wedged (after host sleep) | `docker restart <container>` |
| API 500s `pre-login handshake` | SQL container mid-restart | EF retries 3×; persistent → restart `edgepulse-sqlserver` |
| Dashboard zeros after login | Missing Keycloak protocol mappers | Authentication guide §2 |
| No new telemetry | Agent down, or RabbitMQ relay wedged | Check agent logs, RabbitMQ UI, restart in order |
| Alerts fire but no email | `Smtp:Enabled`/host wrong; check processor logs `Alert email sent` vs `Failed` | Fix Smtp config |
| Webhook `error: timeout` | Receiver unreachable from the processor host | Network/URL; use Send test |
| Build `MSB3021` file locks (dev) | Running API/processor holds DLLs | Stop processes, rebuild |
| `dotnet ef` warnings about tool version | Newer CLI vs pinned EF 9.0.5 | Harmless; commands complete |

## 6. Data retention

Telemetry grows unbounded by default. Options: Mongo TTL index on
`Timestamp` (e.g. 180 d), or scheduled `deleteMany` before a dump. Alerts,
work orders and audit rows are small; keep them for compliance.
