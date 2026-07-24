using EdgePulse.Application.Common;
using EdgePulse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Reports.Queries;

/// <summary>
/// Full alert detail export for a date range as CSV. Role-scoped like the
/// comparison report (MillManager sees their mill only).
/// </summary>
public record ExportAlertsCsvQuery(
    DateTime From,
    DateTime To
) : IRequest<string>;

public class ExportAlertsCsvQueryHandler
    : IRequestHandler<ExportAlertsCsvQuery, string>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ExportAlertsCsvQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<string> Handle(
        ExportAlertsCsvQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Alerts
            .Where(a =>
                a.TenantId == _currentUser.TenantId &&
                !a.IsDeleted &&
                a.TriggeredAt >= request.From &&
                a.TriggeredAt <= request.To);

        if (_currentUser.IsMillManager && _currentUser.MillId is not null)
            query = query.Where(a => a.MillId == _currentUser.MillId);

        var rows = await query
            .OrderByDescending(a => a.TriggeredAt)
            .Join(_context.Devices,
                a => a.DeviceId, d => d.Id,
                (a, d) => new
                {
                    a.TriggeredAt, DeviceName = d.Name, DeviceCode = d.Code,
                    a.MetricKey, a.TriggerValue, a.ThresholdValue, a.Unit,
                    a.SeverityCode, a.StatusCode,
                    a.AcknowledgedAt, a.AcknowledgedBy,
                    a.ResolvedAt, a.ResolvedBy, a.Notes
                })
            .ToListAsync(cancellationToken);

        return CsvBuilder.Build(
            ["TriggeredAt (UTC)", "Device", "Code", "Metric", "Value",
             "Threshold", "Unit", "Severity", "Status",
             "AcknowledgedAt", "AcknowledgedBy", "ResolvedAt", "ResolvedBy", "Notes"],
            rows.Select(r => (IEnumerable<object?>)
            [
                r.TriggeredAt, r.DeviceName, r.DeviceCode, r.MetricKey,
                r.TriggerValue, r.ThresholdValue, r.Unit,
                r.SeverityCode, r.StatusCode,
                r.AcknowledgedAt, r.AcknowledgedBy,
                r.ResolvedAt, r.ResolvedBy, r.Notes
            ]));
    }
}
