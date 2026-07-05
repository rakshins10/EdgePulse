# Sprint 8 — Alerts Engine

**Branch:** `feature/sprint-8-alerts-engine`
**Commit:** `77d69ff`
**Status:** ✅ Complete

---

## Goals

Build the full alerts engine: from threshold configuration through consecutive-breach
detection in the TelemetryProcessor to alert lifecycle management in the API and
a live alerts page in the frontend with a sidebar badge.

---

## What Was Built

### Domain Layer

**`AlertThreshold`** entity — configures when an alert fires for a device metric:
- `DeviceId`, `MetricKey` (e.g. `"temperature"`)
- `MinValue?`, `MaxValue?` — breach condition (at least one required)
- `SeverityCode` — `CRITICAL | HIGH | MEDIUM | LOW` (string, not FK, for TelemetryProcessor access)
- `ConsecutiveCount` — how many consecutive breaches before firing (default: 3)
- `IsActive` — deactivate without deleting

**`Alert`** entity — a fired alert instance:
- Links back to `AlertThreshold`, `Device`, `Mill`, `Area`
- `TriggerValue`, `ThresholdValue` — the reading that fired and what it crossed
- `SeverityCode`, `StatusCode` — both stored as strings for direct SQL access
- `ReadingsJson` — JSON snapshot of the breach readings
- `AiSummary` — reserved for Sprint 9+ AI integration
- Full audit: `AcknowledgedBy`, `AcknowledgedAt`, `ResolvedBy`, `ResolvedAt`, `Notes`

**Alert state machine:**
```
OPEN → ACKNOWLEDGED → RESOLVED → CLOSED
OPEN →               RESOLVED (direct, auto-sets AcknowledgedBy)
```
Resolved and Closed are terminal — `InvalidOperationException` thrown if violated.

---

### Infrastructure

- `AlertThresholdConfiguration` — indexes on `(DeviceId, MetricKey, IsActive)` and `TenantId`
- `AlertConfiguration` — indexes on `(TenantId, StatusCode, TriggeredAt)` and `(TenantId, SeverityCode, StatusCode)`
- EF migration `Sprint8_AlertEngine` — creates `AlertThresholds` and `Alerts` tables
- `EdgePulseDbContext` updated with `DbSet<Alert>` and `DbSet<AlertThreshold>`
- `IApplicationDbContext` updated with matching `IQueryable<T>` properties

---

### Application (CQRS)

**AlertThreshold commands/queries:**
| File | Purpose |
|------|---------|
| `CreateAlertThresholdCommand` | Create threshold, validate device ownership, role-scoped (no Operator/Executive) |
| `UpdateAlertThresholdCommand` | Update threshold, same scope rules |
| `DeleteAlertThresholdCommand` | Soft-delete + deactivate (historical alerts preserved) |
| `GetAlertThresholdsQuery` | List thresholds, filtered by device, role-scoped |

**Alert commands/queries:**
| File | Purpose |
|------|---------|
| `AcknowledgeAlertCommand` | Transition OPEN → ACKNOWLEDGED, area/mill scope checks |
| `ResolveAlertCommand` | Transition to RESOLVED, auto-acknowledges if from OPEN |
| `GetAlertsQuery` | Paginated list (50/page), filters: mill, device, severity, status |
| `GetAlertCountQuery` | Returns `OpenCount` + `CriticalOpenCount` for sidebar badge |

---

### API — `AlertsController`

```
GET    /api/alerts                     — paginated alert list
GET    /api/alerts/count               — badge counts (OPEN, CRITICAL)
POST   /api/alerts/{id}/acknowledge    — acknowledge with optional notes
POST   /api/alerts/{id}/resolve        — resolve with optional notes
GET    /api/alerts/thresholds          — list thresholds
POST   /api/alerts/thresholds          — create threshold
PUT    /api/alerts/thresholds/{id}     — update threshold
DELETE /api/alerts/thresholds/{id}     — soft-delete threshold
```

---

### TelemetryProcessor (fully reconstructed)

Source files were missing from git — rebuilt from scratch.

**Architecture:**

```
RabbitMQ (telemetry.readings)
  ↓
Worker.HandleMessageAsync
  ├─ MongoDB: InsertOneAsync (time-series store)
  └─ AlertEngineService.EvaluateAsync
        ├─ ThresholdCacheService.GetThresholdsAsync (60s cache)
        └─ Per (deviceId, metricKey, thresholdId):
              - ConcurrentDictionary breach counter
              - Fire alert when count >= ConsecutiveCount
              - Deduplication: skip if OPEN alert already exists
              - Reset counter when reading returns to normal
```

**ThresholdCacheService:**
- Raw ADO.NET SQL query (`SELECT` from `AlertThresholds WHERE IsActive=1`)
- `ConcurrentDictionary<string, List<ThresholdCacheEntry>>` keyed by `"{deviceId}:{metricKey}"`
- Refresh interval configurable via `AlertEngine:ThresholdCacheRefreshSeconds` (default 60)
- Stale cache on SQL error — logs error, keeps serving existing thresholds

**AlertEngineService:**
- `_breachCounters` — `ConcurrentDictionary<string, int>` per `"{deviceId}:{metricKey}:{thresholdId}"`
- `_openAlertKeys` — tracks which combinations currently have an open alert (dedup)
- Alert created via raw `INSERT INTO Alerts` — no EF dependency in processor
- `ReadingsJson` — JSON snapshot of the trigger reading: `[{"timestamp":"...","value":42.5}]`

**Project dependencies:** `RabbitMQ.Client 7.1.2`, `MongoDB.Driver 3.4.0`, `Microsoft.EntityFrameworkCore.SqlServer 9.0.5`

---

### Frontend (fully reconstructed + Sprint 8 features)

Source files were missing from git — rebuilt from scratch.

**Project structure:**
```
src/EdgePulse.Dashboard/
├── package.json
├── vite.config.ts          — proxy /api → localhost:5170
├── tsconfig*.json
├── index.html
└── src/
    ├── main.tsx            — Keycloak bootstrap, Redux + Query providers
    ├── App.tsx             — React Router browser router
    ├── index.css           — global reset, dark base
    ├── keycloak.ts         — Keycloak JS config (env vars)
    ├── vite-env.d.ts       — CSS module + Vite env type declarations
    ├── api/
    │   ├── client.ts       — Axios with Keycloak bearer token interceptor
    │   └── alerts.ts       — fetchAlerts, fetchAlertCount, acknowledgeAlert, resolveAlert
    ├── store/
    │   ├── index.ts        — Redux store
    │   ├── hooks.ts        — typed useAppDispatch, useAppSelector
    │   └── alertsSlice.ts  — alert count state (openCount, criticalOpenCount)
    ├── types/
    │   └── alerts.ts       — AlertDto, AlertListResult, AlertCountDto, AlertThresholdDto
    ├── components/
    │   ├── layout/
    │   │   ├── AppLayout   — sidebar + topbar + outlet
    │   │   └── Sidebar     — nav with 30s badge polling
    │   └── alerts/
    │       └── AlertActionModal — acknowledge/resolve with notes
    └── pages/
        ├── alerts/
        │   └── AlertsPage  — filters, paginated table, summary cards
        └── placeholder/
            └── PlaceholderPage — stub for upcoming pages
```

**Styling:** 100% plain CSS Modules, dark theme (`#080b12` base), no Tailwind, no CSS frameworks.

**Sidebar badge:**
- Polls `GET /api/alerts/count` every 30 seconds
- Red badge = CRITICAL open count
- Orange badge = total open count (minus critical)
- Badge cleared when `openCount === 0`

**Alerts page features:**
- Severity filter, status filter (defaults to OPEN)
- Summary cards: total, critical open, high open, medium open
- Paginated table (50/page): device code/name, mill, metric, trigger value vs threshold, severity pill, status pill, relative timestamp
- Acknowledge button on OPEN rows → AlertActionModal
- Resolve button on OPEN/ACKNOWLEDGED rows → AlertActionModal
- TanStack Query with 15s stale time, auto-invalidate after action

---

## Database Changes

```sql
CREATE TABLE AlertThresholds (
  Id             uniqueidentifier NOT NULL PRIMARY KEY,
  TenantId       uniqueidentifier NOT NULL,
  DeviceId       uniqueidentifier NOT NULL REFERENCES Devices(Id) ON DELETE CASCADE,
  MetricKey      nvarchar(100)    NOT NULL,
  Name           nvarchar(200)    NOT NULL,
  MinValue       float            NULL,
  MaxValue       float            NULL,
  Unit           nvarchar(20)     NULL,
  SeverityCode   nvarchar(20)     NOT NULL,
  ConsecutiveCount int            NOT NULL DEFAULT 3,
  IsActive       bit              NOT NULL DEFAULT 1,
  Description    nvarchar(500)    NULL,
  -- BaseEntity fields: CreatedAt, UpdatedAt, IsDeleted, DeletedAt
);

CREATE TABLE Alerts (
  Id                 uniqueidentifier NOT NULL PRIMARY KEY,
  TenantId           uniqueidentifier NOT NULL,
  DeviceId           uniqueidentifier NOT NULL REFERENCES Devices(Id) ON DELETE CASCADE,
  MillId             uniqueidentifier NOT NULL,
  AreaId             uniqueidentifier NOT NULL,
  AlertThresholdId   uniqueidentifier NOT NULL REFERENCES AlertThresholds(Id),
  MetricKey          nvarchar(100)    NOT NULL,
  TriggerValue       float            NOT NULL,
  ThresholdValue     float            NOT NULL,
  Unit               nvarchar(20)     NULL,
  SeverityCode       nvarchar(20)     NOT NULL,
  StatusCode         nvarchar(20)     NOT NULL,
  ReadingsJson       nvarchar(max)    NULL,
  AiSummary          nvarchar(2000)   NULL,
  TriggeredAt        datetime2        NOT NULL,
  AcknowledgedAt     datetime2        NULL,
  AcknowledgedBy     nvarchar(200)    NULL,
  ResolvedAt         datetime2        NULL,
  ResolvedBy         nvarchar(200)    NULL,
  Notes              nvarchar(1000)   NULL,
  -- BaseEntity fields: CreatedAt, UpdatedAt, IsDeleted, DeletedAt
);
```

---

## How to Test

### 1. Configure a threshold

```bash
# POST /api/alerts/thresholds
{
  "deviceId": "<your-device-id>",
  "metricKey": "temperature",
  "name": "High Temperature",
  "maxValue": 85.0,
  "severityCode": "HIGH",
  "unit": "°C",
  "consecutiveCount": 3
}
```

### 2. Send breach telemetry (3 consecutive readings above 85°C)

The device simulator (EdgePulse.Ingestion) must publish to `telemetry.readings`.
Ensure TelemetryProcessor is running and can reach both SQL Server and MongoDB.

### 3. Check alert was created

```bash
GET /api/alerts?statusCode=OPEN
GET /api/alerts/count
```

### 4. Acknowledge via UI

Navigate to `http://localhost:3000/alerts`, click **Ack** on the open alert.

---

## Configuration

`src/backend/EdgePulse.TelemetryProcessor/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "RabbitMQ":   "amqp://edgepulse:EdgePulse%402026@localhost:5672/edgepulse",
    "MongoDB":    "mongodb://edgepulse:EdgePulse%402026@localhost:27017",
    "SqlServer":  "Server=localhost,1433;Database=EdgePulseDb;..."
  },
  "AlertEngine": {
    "ThresholdCacheRefreshSeconds": 60
  }
}
```

Frontend env vars (`.env.local`):
```
VITE_KEYCLOAK_URL=http://localhost:8080
VITE_KEYCLOAK_REALM=edgepulse
VITE_KEYCLOAK_CLIENT_ID=edgepulse-dashboard
```

---

## Known Gaps / Sprint 9 Backlog

| Item | Priority |
|------|----------|
| AI summary generation for fired alerts (`AiSummary` field is ready) | P1 |
| Alert dedup: reset `_openAlertKeys` from DB on TelemetryProcessor restart | P1 |
| Demo seed data with pre-configured thresholds for NordPulp | P0 |
| ThresholdManagement UI (configure thresholds from the dashboard) | P2 |
| Alert history / analytics charts on dashboard | P2 |
| Email/webhook notifications on CRITICAL alerts | P3 |
