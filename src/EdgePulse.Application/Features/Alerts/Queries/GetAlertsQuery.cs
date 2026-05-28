using EdgePulse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Alerts.Queries;

public record GetAlertsQuery(
    Guid? MillId = null,
    Guid? DeviceId = null,
    string? SeverityCode = null,
    string? StatusCode = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<AlertListResult>;

public record AlertListResult(
    List<AlertDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record AlertDto(
    Guid Id,
    Guid DeviceId,
    string DeviceName,
    string DeviceCode,
    Guid MillId,
    string MillName,
    string MetricKey,
    double TriggerValue,
    double ThresholdValue,
    string? Unit,
    string SeverityCode,
    string StatusCode,
    string? AiSummary,
    DateTime TriggeredAt,
    DateTime? AcknowledgedAt,
    string? AcknowledgedBy,
    DateTime? ResolvedAt,
    string? ResolvedBy,
    string? Notes
);

public class GetAlertsQueryHandler
    : IRequestHandler<GetAlertsQuery, AlertListResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetAlertsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<AlertListResult> Handle(
        GetAlertsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Alerts
            .Where(x => x.TenantId == _currentUser.TenantId);

        // Role-based scoping
        if (_currentUser.IsMillManager && _currentUser.MillId.HasValue)
            query = query.Where(x => x.MillId == _currentUser.MillId.Value);

        if (_currentUser.IsOperator && _currentUser.AreaIds.Any())
            query = query.Where(x => _currentUser.AreaIds.Contains(x.AreaId));

        // Request filters
        if (request.MillId.HasValue)
            query = query.Where(x => x.MillId == request.MillId.Value);

        if (request.DeviceId.HasValue)
            query = query.Where(x => x.DeviceId == request.DeviceId.Value);

        if (!string.IsNullOrEmpty(request.SeverityCode))
            query = query.Where(x => x.SeverityCode == request.SeverityCode.ToUpper());

        if (!string.IsNullOrEmpty(request.StatusCode))
            query = query.Where(x => x.StatusCode == request.StatusCode.ToUpper());

        var totalCount = await query.CountAsync(cancellationToken);

        var pageSize = request.PageSize > 0 && request.PageSize <= 200
            ? request.PageSize : 50;
        var page = request.Page > 0 ? request.Page : 1;

        var items = await query
            .OrderByDescending(x => x.TriggeredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AlertDto(
                x.Id,
                x.DeviceId,
                x.Device!.Name,
                x.Device.Code,
                x.MillId,
                x.Device.Mill!.Name,
                x.MetricKey,
                x.TriggerValue,
                x.ThresholdValue,
                x.Unit,
                x.SeverityCode,
                x.StatusCode,
                x.AiSummary,
                x.TriggeredAt,
                x.AcknowledgedAt,
                x.AcknowledgedBy,
                x.ResolvedAt,
                x.ResolvedBy,
                x.Notes))
            .ToListAsync(cancellationToken);

        return new AlertListResult(items, totalCount, page, pageSize);
    }
}
