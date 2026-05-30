using EdgePulse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Dashboard.Queries;

// ─── Query & DTOs ─────────────────────────────────────────────────────────────

public record GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>;

public record DashboardSummaryDto(
    int TotalDevices,
    int OpenAlerts,
    int CriticalOpenAlerts,
    int DevicesWithAlerts,
    IReadOnlyList<AlertTrendDayDto> AlertTrend,
    IReadOnlyList<SeverityCountDto> BySeverity,
    IReadOnlyList<TopDeviceDto> TopDevices
);

/// <summary>Alert count for a single calendar day (UTC).</summary>
public record AlertTrendDayDto(DateOnly Date, int Count);

/// <summary>Open alert count for a given severity code.</summary>
public record SeverityCountDto(string SeverityCode, int Count);

/// <summary>Device ranked by its number of active (open/acknowledged) alerts.</summary>
public record TopDeviceDto(
    Guid DeviceId,
    string DeviceCode,
    string DeviceName,
    string MillName,
    int AlertCount
);

// ─── Handler ──────────────────────────────────────────────────────────────────

public class GetDashboardSummaryQueryHandler
    : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    // Active = not yet resolved or closed
    private static readonly string[] ActiveStatuses = ["OPEN", "ACKNOWLEDGED"];

    public GetDashboardSummaryQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<DashboardSummaryDto> Handle(
        GetDashboardSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        // ── Base queryables (role-scoped) ──────────────────────────────────────

        var deviceQuery = _context.Devices
            .Where(d => d.TenantId == tenantId);

        var alertQuery = _context.Alerts
            .Where(a => a.TenantId == tenantId);

        // MillManager sees only their mill
        if (_currentUser.IsMillManager && _currentUser.MillId.HasValue)
        {
            var millId = _currentUser.MillId.Value;
            deviceQuery = deviceQuery.Where(d => d.MillId == millId);
            alertQuery  = alertQuery.Where(a => a.MillId == millId);
        }

        // Operator sees only their assigned areas
        if (_currentUser.IsOperator && _currentUser.AreaIds.Any())
        {
            var areaIds = _currentUser.AreaIds;
            deviceQuery = deviceQuery.Where(d => areaIds.Contains(d.AreaId));
            alertQuery  = alertQuery.Where(a => areaIds.Contains(a.AreaId));
        }

        var activeAlertQuery = alertQuery
            .Where(a => ActiveStatuses.Contains(a.StatusCode));

        // ── 1. Total devices ───────────────────────────────────────────────────

        var totalDevices = await deviceQuery.CountAsync(cancellationToken);

        // ── 2. Open / critical open alert counts ──────────────────────────────

        var openAlerts = await activeAlertQuery.CountAsync(cancellationToken);

        var criticalOpenAlerts = await activeAlertQuery
            .Where(a => a.SeverityCode == "CRITICAL")
            .CountAsync(cancellationToken);

        // ── 3. Distinct devices with at least one active alert ─────────────────

        var devicesWithAlerts = await activeAlertQuery
            .Select(a => a.DeviceId)
            .Distinct()
            .CountAsync(cancellationToken);

        // ── 4. 7-day alert trend ───────────────────────────────────────────────
        // Count ALL (including resolved) alerts triggered in the last 7 days.
        // This gives the exec a true picture of incident frequency.

        var since = DateTime.UtcNow.Date.AddDays(-6); // today - 6 = 7 days inclusive

        var trendRaw = await alertQuery
            .Where(a => a.TriggeredAt >= since)
            .GroupBy(a => a.TriggeredAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        // Fill in missing days with zero so the chart always shows 7 bars
        var alertTrend = Enumerable.Range(0, 7)
            .Select(i =>
            {
                var date = DateOnly.FromDateTime(since.AddDays(i));
                var count = trendRaw.FirstOrDefault(r => r.Date == since.AddDays(i))?.Count ?? 0;
                return new AlertTrendDayDto(date, count);
            })
            .ToList();

        // ── 5. Active alerts by severity ──────────────────────────────────────

        var severityRaw = await activeAlertQuery
            .GroupBy(a => a.SeverityCode)
            .Select(g => new { SeverityCode = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        // Ensure all four severity levels are present (even at zero) and in order
        var severityOrder = new[] { "CRITICAL", "HIGH", "MEDIUM", "LOW" };
        var bySeverity = severityOrder
            .Select(code => new SeverityCountDto(
                code,
                severityRaw.FirstOrDefault(s => s.SeverityCode == code)?.Count ?? 0))
            .ToList();

        // ── 6. Top 5 devices by active alert count ────────────────────────────

        var topDevicesRaw = await activeAlertQuery
            .GroupBy(a => a.DeviceId)
            .Select(g => new { DeviceId = g.Key, AlertCount = g.Count() })
            .OrderByDescending(g => g.AlertCount)
            .Take(5)
            .ToListAsync(cancellationToken);

        var topDeviceIds = topDevicesRaw.Select(t => t.DeviceId).ToList();

        var deviceInfo = await _context.Devices
            .Where(d => topDeviceIds.Contains(d.Id))
            .Select(d => new
            {
                d.Id,
                d.Code,
                d.Name,
                MillName = d.Mill!.Name,
            })
            .ToListAsync(cancellationToken);

        var topDevices = topDevicesRaw
            .Select(t =>
            {
                var info = deviceInfo.FirstOrDefault(d => d.Id == t.DeviceId);
                return new TopDeviceDto(
                    t.DeviceId,
                    info?.Code ?? "–",
                    info?.Name ?? "Unknown Device",
                    info?.MillName ?? "–",
                    t.AlertCount);
            })
            .ToList();

        return new DashboardSummaryDto(
            totalDevices,
            openAlerts,
            criticalOpenAlerts,
            devicesWithAlerts,
            alertTrend,
            bySeverity,
            topDevices);
    }
}
