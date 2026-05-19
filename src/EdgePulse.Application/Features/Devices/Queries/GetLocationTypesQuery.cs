using EdgePulse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Devices.Queries;

public record GetLocationTypesQuery : IRequest<List<LocationTypeDto>>;

public record LocationTypeDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    bool IsSystem,
    int SortOrder
);

public class GetLocationTypesQueryHandler
    : IRequestHandler<GetLocationTypesQuery, List<LocationTypeDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetLocationTypesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<LocationTypeDto>> Handle(
        GetLocationTypesQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.LocationTypes
            .Where(x => !x.IsDeleted && x.IsActive)
            .Where(x => x.TenantId == null ||
                        x.TenantId == _currentUser.TenantId)
            .OrderBy(x => x.IsSystem ? 0 : 1)
            .ThenBy(x => x.SortOrder)
            .Select(x => new LocationTypeDto(
                x.Id, x.Name, x.Code,
                x.Description, x.IsSystem, x.SortOrder))
            .ToListAsync(cancellationToken);
    }
}
