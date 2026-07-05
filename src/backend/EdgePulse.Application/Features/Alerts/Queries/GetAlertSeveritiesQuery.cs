using EdgePulse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Alerts.Queries;

public record GetAlertSeveritiesQuery : IRequest<List<AlertSeverityDto>>;

public record AlertSeverityDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    string? Color,
    int Priority,
    bool IsSystem,
    int SortOrder
);

public class GetAlertSeveritiesQueryHandler
    : IRequestHandler<GetAlertSeveritiesQuery, List<AlertSeverityDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetAlertSeveritiesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<AlertSeverityDto>> Handle(
        GetAlertSeveritiesQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.AlertSeverities
            .Where(x => !x.IsDeleted && x.IsActive)
            .Where(x => x.TenantId == null ||
                        x.TenantId == _currentUser.TenantId)
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.SortOrder)
            .Select(x => new AlertSeverityDto(
                x.Id, x.Name, x.Code, x.Description,
                x.Color, x.Priority, x.IsSystem, x.SortOrder))
            .ToListAsync(cancellationToken);
    }
}
