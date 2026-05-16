using EdgePulse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Devices.Queries;

public record GetDeviceStatusesQuery : IRequest<List<DeviceStatusDto>>;

public record DeviceStatusDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    string? Color,
    bool IsSystem,
    int SortOrder
);

public class GetDeviceStatusesQueryHandler
    : IRequestHandler<GetDeviceStatusesQuery, List<DeviceStatusDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetDeviceStatusesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<DeviceStatusDto>> Handle(
        GetDeviceStatusesQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.DeviceStatuses
            .Where(x => !x.IsDeleted && x.IsActive)
            .Where(x => x.TenantId == null ||
                        x.TenantId == _currentUser.TenantId)
            .OrderBy(x => x.IsSystem ? 0 : 1)
            .ThenBy(x => x.SortOrder)
            .Select(x => new DeviceStatusDto(
                x.Id, x.Name, x.Code,
                x.Description, x.Color,
                x.IsSystem, x.SortOrder))
            .ToListAsync(cancellationToken);
    }
}
