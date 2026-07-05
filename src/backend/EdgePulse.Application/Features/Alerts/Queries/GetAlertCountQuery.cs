using EdgePulse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Alerts.Queries;

/// <summary>
/// Returns the count of OPEN alerts for the sidebar badge.
/// Scoped by role: MillManager sees their mill,
/// Operator sees their assigned areas.
/// Optionally filtered to CRITICAL only for the danger badge.
/// </summary>
public record GetAlertCountQuery(
    string? SeverityCode = null
) : IRequest<AlertCountDto>;

public record AlertCountDto(
    int OpenCount,
    int CriticalOpenCount
);

public class GetAlertCountQueryHandler
    : IRequestHandler<GetAlertCountQuery, AlertCountDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetAlertCountQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<AlertCountDto> Handle(
        GetAlertCountQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Alerts
            .Where(x => x.TenantId == _currentUser.TenantId &&
                        x.StatusCode == "OPEN");

        // Role-based scoping
        if (_currentUser.IsMillManager && _currentUser.MillId.HasValue)
            query = query.Where(x => x.MillId == _currentUser.MillId.Value);

        if (_currentUser.IsOperator && _currentUser.AreaIds.Any())
            query = query.Where(x => _currentUser.AreaIds.Contains(x.AreaId));

        var openCount = await query.CountAsync(cancellationToken);
        var criticalCount = await query
            .Where(x => x.SeverityCode == "CRITICAL")
            .CountAsync(cancellationToken);

        return new AlertCountDto(openCount, criticalCount);
    }
}
