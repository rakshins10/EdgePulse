using EdgePulse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Reports.Queries;

public record GetMillComparisonReportQuery(
    DateTime From,
    DateTime To
) : IRequest<MillComparisonReport>;

public record MillComparisonReport(
    DateTime From,
    DateTime To,
    DateTime GeneratedAt,
    List<MillReportRow> Mills
);

public record MillReportRow(
    Guid MillId,
    string MillName,
    string Location,
    int DeviceCount,
    int TotalAlerts,
    int OpenAlerts,
    int CriticalAlerts,
    int HighAlerts,
    double? AvgAcknowledgeMinutes,
    double? AvgResolveMinutes
);

/// <summary>
/// Cross-mill operational comparison over a date range. Alert figures count
/// alerts *triggered* in the range; OpenAlerts is the current live number.
/// MTTA / MTTR are computed from acknowledged / resolved alerts in the range.
/// MillManager sees only their own mill; other roles see every mill.
/// </summary>
public class GetMillComparisonReportQueryHandler
    : IRequestHandler<GetMillComparisonReportQuery, MillComparisonReport>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetMillComparisonReportQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<MillComparisonReport> Handle(
        GetMillComparisonReportQuery request,
        CancellationToken cancellationToken)
    {
        var millsQuery = _context.Mills
            .Where(m => m.TenantId == _currentUser.TenantId && !m.IsDeleted);

        if (_currentUser.IsMillManager && _currentUser.MillId is not null)
            millsQuery = millsQuery.Where(m => m.Id == _currentUser.MillId);

        var mills = await millsQuery
            .Select(m => new { m.Id, m.Name, m.Location })
            .ToListAsync(cancellationToken);

        var millIds = mills.Select(m => m.Id).ToList();

        var devices = await _context.Devices
            .Where(d =>
                d.TenantId == _currentUser.TenantId &&
                millIds.Contains(d.MillId) &&
                !d.IsDeleted)
            .Select(d => new { d.MillId })
            .ToListAsync(cancellationToken);

        // Pull the alert fields needed and aggregate in memory — mill counts
        // are small and this keeps the query provider-agnostic (incl. tests).
        var alerts = await _context.Alerts
            .Where(a =>
                a.TenantId == _currentUser.TenantId &&
                millIds.Contains(a.MillId) &&
                !a.IsDeleted &&
                ((a.TriggeredAt >= request.From && a.TriggeredAt <= request.To) ||
                  a.StatusCode == "OPEN" || a.StatusCode == "ACKNOWLEDGED"))
            .Select(a => new
            {
                a.MillId, a.StatusCode, a.SeverityCode,
                a.TriggeredAt, a.AcknowledgedAt, a.ResolvedAt
            })
            .ToListAsync(cancellationToken);

        var rows = mills
            .Select(mill =>
            {
                var inRange = alerts
                    .Where(a => a.MillId == mill.Id &&
                                a.TriggeredAt >= request.From &&
                                a.TriggeredAt <= request.To)
                    .ToList();

                var ackDurations = inRange
                    .Where(a => a.AcknowledgedAt.HasValue)
                    .Select(a => (a.AcknowledgedAt!.Value - a.TriggeredAt).TotalMinutes)
                    .ToList();

                var resolveDurations = inRange
                    .Where(a => a.ResolvedAt.HasValue)
                    .Select(a => (a.ResolvedAt!.Value - a.TriggeredAt).TotalMinutes)
                    .ToList();

                return new MillReportRow(
                    MillId: mill.Id,
                    MillName: mill.Name,
                    Location: mill.Location ?? string.Empty,
                    DeviceCount: devices.Count(d => d.MillId == mill.Id),
                    TotalAlerts: inRange.Count,
                    OpenAlerts: alerts.Count(a =>
                        a.MillId == mill.Id &&
                        (a.StatusCode == "OPEN" || a.StatusCode == "ACKNOWLEDGED")),
                    CriticalAlerts: inRange.Count(a => a.SeverityCode == "CRITICAL"),
                    HighAlerts: inRange.Count(a => a.SeverityCode == "HIGH"),
                    AvgAcknowledgeMinutes: ackDurations.Count > 0
                        ? Math.Round(ackDurations.Average(), 1) : null,
                    AvgResolveMinutes: resolveDurations.Count > 0
                        ? Math.Round(resolveDurations.Average(), 1) : null);
            })
            .OrderBy(r => r.MillName)
            .ToList();

        return new MillComparisonReport(
            request.From, request.To, DateTime.UtcNow, rows);
    }
}
