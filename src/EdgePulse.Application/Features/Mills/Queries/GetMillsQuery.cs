using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Mills.Queries;

public record GetMillsQuery : IRequest<List<MillDto>>;

public record MillDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string Code,
    string Location,
    string Timezone,
    bool HasInternet,
    DeploymentMode DeploymentMode,
    int AreaCount,
    DateTime CreatedAt
);

public class GetMillsQueryHandler
    : IRequestHandler<GetMillsQuery, List<MillDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetMillsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<MillDto>> Handle(
        GetMillsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Mills
            .Where(x => !x.IsDeleted);

        // SuperAdmin sees all mills
        // Others see only their tenant mills
        if (!_currentUser.IsSuperAdmin)
            query = query.Where(x =>
                x.TenantId == _currentUser.TenantId);

        // MillManager sees only their assigned mill
        if (_currentUser.IsMillManager && _currentUser.MillId.HasValue)
            query = query.Where(x =>
                x.Id == _currentUser.MillId.Value);

        return await query
            .OrderBy(x => x.Name)
            .Select(x => new MillDto(
                x.Id,
                x.TenantId,
                x.Name,
                x.Code,
                x.Location,
                x.Timezone,
                x.HasInternet,
                x.DeploymentMode,
                x.Areas.Count(a => !a.IsDeleted),
                x.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
