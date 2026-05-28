using EdgePulse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Alerts.Queries;

public record GetAlertThresholdsQuery(
    Guid? DeviceId = null,
    bool IncludeInactive = false
) : IRequest<List<AlertThresholdDto>>;

public record AlertThresholdDto(
    Guid Id,
    Guid DeviceId,
    string DeviceName,
    string DeviceCode,
    string MetricKey,
    string Name,
    double? MinValue,
    double? MaxValue,
    string? Unit,
    string SeverityCode,
    int ConsecutiveCount,
    bool IsActive,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public class GetAlertThresholdsQueryHandler
    : IRequestHandler<GetAlertThresholdsQuery, List<AlertThresholdDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetAlertThresholdsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<AlertThresholdDto>> Handle(
        GetAlertThresholdsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.AlertThresholds
            .Where(x => !x.IsDeleted &&
                        x.TenantId == _currentUser.TenantId);

        if (!request.IncludeInactive)
            query = query.Where(x => x.IsActive);

        if (request.DeviceId.HasValue)
            query = query.Where(x => x.DeviceId == request.DeviceId.Value);

        // MillManager sees only their mill's thresholds
        if (_currentUser.IsMillManager && _currentUser.MillId.HasValue)
            query = query.Where(x =>
                x.Device!.MillId == _currentUser.MillId.Value);

        // Operator sees only assigned area thresholds
        if (_currentUser.IsOperator && _currentUser.AreaIds.Any())
            query = query.Where(x =>
                _currentUser.AreaIds.Contains(x.Device!.AreaId));

        return await query
            .OrderBy(x => x.Device!.Code)
            .ThenBy(x => x.MetricKey)
            .Select(x => new AlertThresholdDto(
                x.Id,
                x.DeviceId,
                x.Device!.Name,
                x.Device.Code,
                x.MetricKey,
                x.Name,
                x.MinValue,
                x.MaxValue,
                x.Unit,
                x.SeverityCode,
                x.ConsecutiveCount,
                x.IsActive,
                x.Description,
                x.CreatedAt,
                x.UpdatedAt))
            .ToListAsync(cancellationToken);
    }
}
