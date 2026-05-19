using EdgePulse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Devices.Queries;

public record GetMaintenanceTypesQuery : IRequest<List<MaintenanceTypeDto>>;

public record MaintenanceTypeDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    string? Color,
    bool IsSystem,
    int SortOrder
);

public class GetMaintenanceTypesQueryHandler
    : IRequestHandler<GetMaintenanceTypesQuery, List<MaintenanceTypeDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetMaintenanceTypesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<MaintenanceTypeDto>> Handle(
        GetMaintenanceTypesQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.MaintenanceTypes
            .Where(x => !x.IsDeleted && x.IsActive)
            .Where(x => x.TenantId == null ||
                        x.TenantId == _currentUser.TenantId)
            .OrderBy(x => x.IsSystem ? 0 : 1)
            .ThenBy(x => x.SortOrder)
            .Select(x => new MaintenanceTypeDto(
                x.Id, x.Name, x.Code,
                x.Description, x.Color,
                x.IsSystem, x.SortOrder))
            .ToListAsync(cancellationToken);
    }
}
