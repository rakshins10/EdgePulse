using EdgePulse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Areas.Queries;

public record GetAreasQuery(Guid? MillId = null)
    : IRequest<List<AreaDto>>;

public record AreaDto(
    Guid Id,
    Guid MillId,
    string MillName,
    string Name,
    string Code,
    string? Description,
    string? LocationTypeName,
    int DeviceCount,
    DateTime CreatedAt
);

public class GetAreasQueryHandler
    : IRequestHandler<GetAreasQuery, List<AreaDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetAreasQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<AreaDto>> Handle(
        GetAreasQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Areas
            .Where(x => !x.IsDeleted &&
                        x.TenantId == _currentUser.TenantId);

        if (request.MillId.HasValue)
            query = query.Where(x => x.MillId == request.MillId.Value);

        // MillManager sees only their mill areas
        if (_currentUser.IsMillManager && _currentUser.MillId.HasValue)
            query = query.Where(x =>
                x.MillId == _currentUser.MillId.Value);

        // Operator sees only assigned areas
        if (_currentUser.IsOperator && _currentUser.AreaIds.Any())
            query = query.Where(x =>
                _currentUser.AreaIds.Contains(x.Id));

        return await query
            .OrderBy(x => x.Mill!.Name)
            .ThenBy(x => x.Name)
            .Select(x => new AreaDto(
                x.Id,
                x.MillId,
                x.Mill!.Name,
                x.Name,
                x.Code,
                x.Description,
                x.LocationType != null ? x.LocationType.Name : null,
                x.Devices.Count(d => !d.IsDeleted),
                x.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
