using EdgePulse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Devices.Queries;

public record GetDeviceTypesQuery : IRequest<List<DeviceTypeDto>>;

public record DeviceTypeDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    string? Icon,
    bool IsSystem,
    int SortOrder
);

public class GetDeviceTypesQueryHandler
    : IRequestHandler<GetDeviceTypesQuery, List<DeviceTypeDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetDeviceTypesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<DeviceTypeDto>> Handle(
        GetDeviceTypesQuery request,
        CancellationToken cancellationToken)
    {
        var deviceTypes = await _context.DeviceTypes
            .Where(x => !x.IsDeleted && x.IsActive)
            .Where(x =>
                x.TenantId == null ||
                x.TenantId == _currentUser.TenantId)
            .OrderBy(x => x.IsSystem ? 0 : 1)
            .ThenBy(x => x.SortOrder)
            .Select(x => new DeviceTypeDto(
                x.Id,
                x.Name,
                x.Code,
                x.Description,
                x.Icon,
                x.IsSystem,
                x.SortOrder))
            .ToListAsync(cancellationToken);

        return deviceTypes;
    }
}
