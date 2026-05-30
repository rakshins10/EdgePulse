# Sprint 12 — Executive Dashboard

**Status:** Done  
**Branch:** `feature/sprint-12-executive-dashboard`  
**GitHub Issue:** #68  
**Date completed:** 2026-05-31

---

## Goal

Build a read-only executive dashboard that surfaces the most operationally significant KPIs, an alert trend chart, severity distribution, and the devices generating the most alerts — all without any third-party chart library.

---

## What was built

### Backend — `EdgePulse.Application`

**`Features/Dashboard/Queries/GetDashboardSummaryQuery.cs`**

Single CQRS query that executes six async database reads and returns `DashboardSummaryDto`:

| Field | Type | Description |
|-------|------|-------------|
| `TotalDevices` | `int` | Active devices in role-scoped view |
| `OpenAlerts` | `int` | OPEN + ACKNOWLEDGED alert count |
| `CriticalOpenAlerts` | `int` | Subset: CRITICAL severity |
| `DevicesWithAlerts` | `int` | Distinct devices with ≥1 active alert |
| `AlertTrend` | `IReadOnlyList<AlertTrendDayDto>` | 7 days (oldest → today), fills zeros for quiet days |
| `BySeverity` | `IReadOnlyList<SeverityCountDto>` | CRITICAL / HIGH / MEDIUM / LOW counts, always 4 entries |
| `TopDevices` | `IReadOnlyList<TopDeviceDto>` | Top 5 devices by active alert count with code + name + mill |

Role scoping mirrors all other queries:
- SuperAdmin / CustomerAdmin / Executive — full tenant view
- MillManager — their assigned mill only
- Operator — their assigned areas only

The trend window uses `DateTime.UtcNow.Date.AddDays(-6)` to produce a full 7-day inclusive range starting 6 days ago. Missing days are filled with zero on the .NET side so the frontend never needs to deal with sparse arrays.

### Backend — `EdgePulse.API`

**`Controllers/DashboardController.cs`** — `GET /api/dashboard/summary`

- `[Authorize]` — all authenticated roles
- Returns `DashboardSummaryDto` as 200 JSON
- XML doc comments explain the role-scoping semantics

---

### Frontend — `EdgePulse.Dashboard`

**`src/types/dashboard.ts`**  
TypeScript interfaces mirroring the C# DTOs:
- `DashboardSummaryDto`, `AlertTrendDay`, `SeverityCount`, `TopDevice`

**`src/api/dashboard.ts`**  
Single function `fetchDashboardSummary()` over the shared axios client.

**`src/components/dashboard/KpiTile.tsx` + `KpiTile.module.css`**  
Reusable tile with four accent variants:
- `default` (blue left-border) — neutral metric
- `critical` (red) — red value when non-zero
- `warning` (orange) — orange value when non-zero
- `ok` (green) — used when the count is zero

**`src/components/dashboard/AlertTrendChart.tsx` + `AlertTrendChart.module.css`**  
Pure SVG 7-day bar chart, zero dependencies:
- Fixed 600×180 viewBox, scales via `viewBox` + CSS `width: 100%`
- Y-axis gridlines with dashed stroke; 3 tick labels (0, mid, max)
- Bar labels (count) rendered above non-zero bars
- X-axis: two-line label — weekday abbreviation + "May 29" date string
- Empty bars (count = 0) rendered as a 2 px stub in `--color-border`
- Y ceiling snapped to nearest multiple of 5 for cleaner labels

**`src/components/dashboard/SeverityChart.tsx` + `SeverityChart.module.css`**  
CSS horizontal bar chart (no SVG):
- `ul`/`li` list, grid layout: label | bar track | count
- Bar width is `(count / total) * 100%`, minimum 2 px to remain visible
- Colour-coded fills using `--color-critical / high / medium / low`
- Zero-alert state replaced by "No active alerts — all clear." in green

**`src/components/dashboard/TopDevicesTable.tsx` + `TopDevicesTable.module.css`**  
Ranked table, top 5 devices:
- Rank column (1-5) + device code (monospace, accent colour) + name + mill + inline bar + count
- Bar width relative to the device with the most alerts
- Hover row highlight via `--color-row-hover`

**`src/pages/DashboardPage.tsx` + `DashboardPage.module.css`**  
Composes all components:
- Fetches via React Query: `staleTime: 30s`, auto-refetch every 60 seconds
- KPI row: responsive grid — 4-col → 2-col (≤1024 px) → 1-col (≤480 px)
- Charts row: 3:2 split → stacked (≤900 px)
- Loading state: CSS-only spinner via border animation
- Error state: red text with message from `Error.message`

**`src/App.tsx`**  
- Replaced `PlaceholderPage` import with `DashboardPage`
- Changed default redirect from `/alerts` to `/dashboard`

---

## Design decisions

| Decision | Rationale |
|----------|-----------|
| MTBF replaced with "Devices at Risk" | MTBF requires maintenance downtime records not yet in the data model. `DevicesWithAlerts` is computable from existing data and equally meaningful. |
| Alert trend counts ALL statuses (incl. resolved) | Gives the exec true incident frequency. Counting only active alerts would undercount past spikes that have since been resolved. |
| Zero-fill missing trend days on server | The server always returns exactly 7 days; the client never needs to iterate over a sparse array to find gaps. |
| No chart library | Project constraint (no CSS frameworks, no chart libs). The plain-SVG bar chart is ~100 lines and fully theme-aware. |
| 60s refetch interval | Dashboard data is not real-time; a 1-minute poll is sufficient for the exec view without hammering the API. |

---

## Files changed

```
src/EdgePulse.Application/Features/Dashboard/Queries/GetDashboardSummaryQuery.cs  (new)
src/EdgePulse.API/Controllers/DashboardController.cs                               (new)
src/EdgePulse.Dashboard/src/types/dashboard.ts                                     (new)
src/EdgePulse.Dashboard/src/api/dashboard.ts                                       (new)
src/EdgePulse.Dashboard/src/components/dashboard/KpiTile.tsx                       (new)
src/EdgePulse.Dashboard/src/components/dashboard/KpiTile.module.css                (new)
src/EdgePulse.Dashboard/src/components/dashboard/AlertTrendChart.tsx               (new)
src/EdgePulse.Dashboard/src/components/dashboard/AlertTrendChart.module.css        (new)
src/EdgePulse.Dashboard/src/components/dashboard/SeverityChart.tsx                 (new)
src/EdgePulse.Dashboard/src/components/dashboard/SeverityChart.module.css          (new)
src/EdgePulse.Dashboard/src/components/dashboard/TopDevicesTable.tsx               (new)
src/EdgePulse.Dashboard/src/components/dashboard/TopDevicesTable.module.css        (new)
src/EdgePulse.Dashboard/src/pages/DashboardPage.tsx                                (new)
src/EdgePulse.Dashboard/src/pages/DashboardPage.module.css                         (new)
src/EdgePulse.Dashboard/src/App.tsx                                                (modified)
docs/sprints/sprint-12-executive-dashboard.md                                      (new)
```

---

## API endpoint

```
GET /api/dashboard/summary
Authorization: Bearer <keycloak-token>

200 OK
{
  "totalDevices": 20,
  "openAlerts": 7,
  "criticalOpenAlerts": 2,
  "devicesWithAlerts": 5,
  "alertTrend": [
    { "date": "2026-05-25", "count": 3 },
    { "date": "2026-05-26", "count": 1 },
    { "date": "2026-05-27", "count": 0 },
    { "date": "2026-05-28", "count": 4 },
    { "date": "2026-05-29", "count": 2 },
    { "date": "2026-05-30", "count": 5 },
    { "date": "2026-05-31", "count": 1 }
  ],
  "bySeverity": [
    { "severityCode": "CRITICAL", "count": 2 },
    { "severityCode": "HIGH",     "count": 3 },
    { "severityCode": "MEDIUM",   "count": 2 },
    { "severityCode": "LOW",      "count": 0 }
  ],
  "topDevices": [
    { "deviceId": "...", "deviceCode": "LW-FWP-001", "deviceName": "...", "millName": "Lakewood", "alertCount": 3 }
  ]
}
```
