using EdgePulse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Alerts.Queries;

public record GetAlertStatusesQuery : IRequest<List<AlertStatusDto>>;

public record AlertStatusDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    bool IsTerminal,
    bool IsSystem,
    int SortOrder
);

public class GetAlertStatusesQueryHandler
    : IRequestHandler<GetAlertStatusesQuery, List<AlertStatusDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetAlertStatusesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<AlertStatusDto>> Handle(
        GetAlertStatusesQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.AlertStatuses
            .Where(x => !x.IsDeleted && x.IsActive)
            .Where(x => x.TenantId == null ||
                        x.TenantId == _currentUser.TenantId)
            .OrderBy(x => x.IsSystem ? 0 : 1)
            .ThenBy(x => x.SortOrder)
            .Select(x => new AlertStatusDto(
                x.Id, x.Name, x.Code, x.Description,
                x.IsTerminal, x.IsSystem, x.SortOrder))
            .ToListAsync(cancellationToken);
    }
}
