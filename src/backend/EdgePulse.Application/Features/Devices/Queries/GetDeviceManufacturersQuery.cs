using EdgePulse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Devices.Queries;

public record GetDeviceManufacturersQuery : IRequest<List<DeviceManufacturerDto>>;

public record DeviceManufacturerDto(
    Guid Id,
    string Name,
    string Code,
    string? Website,
    string? Country,
    bool IsSystem,
    int SortOrder
);

public class GetDeviceManufacturersQueryHandler
    : IRequestHandler<GetDeviceManufacturersQuery, List<DeviceManufacturerDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetDeviceManufacturersQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<DeviceManufacturerDto>> Handle(
        GetDeviceManufacturersQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.DeviceManufacturers
            .Where(x => !x.IsDeleted && x.IsActive)
            .Where(x => x.TenantId == null ||
                        x.TenantId == _currentUser.TenantId)
            .OrderBy(x => x.IsSystem ? 0 : 1)
            .ThenBy(x => x.SortOrder)
            .Select(x => new DeviceManufacturerDto(
                x.Id, x.Name, x.Code,
                x.Website, x.Country,
                x.IsSystem, x.SortOrder))
            .ToListAsync(cancellationToken);
    }
}
