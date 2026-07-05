using EdgePulse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Devices.Queries;

public record GetDevicesQuery(
    Guid? AreaId = null,
    Guid? MillId = null
) : IRequest<List<DeviceListDto>>;

public record DeviceListDto(
    Guid Id,
    Guid AreaId,
    string AreaName,
    Guid MillId,
    string MillName,
    string Name,
    string Code,
    string TypeName,
    string StatusName,
    string? StatusColor,
    string? ManufacturerName,
    string? ModelName,
    string? SerialNumber,
    DateTime? LastSeenAt,
    DateTime CreatedAt
);

public class GetDevicesQueryHandler
    : IRequestHandler<GetDevicesQuery, List<DeviceListDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetDevicesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<DeviceListDto>> Handle(
        GetDevicesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Devices
            .Where(x => !x.IsDeleted &&
                        x.TenantId == _currentUser.TenantId);

        if (request.MillId.HasValue)
            query = query.Where(x => x.MillId == request.MillId.Value);

        if (request.AreaId.HasValue)
            query = query.Where(x => x.AreaId == request.AreaId.Value);

        // MillManager sees only their mill devices
        if (_currentUser.IsMillManager && _currentUser.MillId.HasValue)
            query = query.Where(x =>
                x.MillId == _currentUser.MillId.Value);

        // Operator sees only assigned area devices
        if (_currentUser.IsOperator && _currentUser.AreaIds.Any())
            query = query.Where(x =>
                _currentUser.AreaIds.Contains(x.AreaId));

        return await query
            .OrderBy(x => x.Area!.Mill!.Name)
            .ThenBy(x => x.Area!.Name)
            .ThenBy(x => x.Code)
            .Select(x => new DeviceListDto(
                x.Id,
                x.AreaId,
                x.Area!.Name,
                x.MillId,
                x.Area.Mill!.Name,
                x.Name,
                x.Code,
                x.Type!.Name,
                x.Status!.Name,
                x.Status.Color,
                x.Manufacturer != null ? x.Manufacturer.Name : null,
                x.Model != null ? x.Model.Name : null,
                x.SerialNumber,
                x.LastSeenAt,
                x.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
