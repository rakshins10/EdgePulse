using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Constants;
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
    private readonly ILookupTranslator _translator;

    public GetDeviceStatusesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ILookupTranslator translator)
    {
        _context = context;
        _currentUser = currentUser;
        _translator = translator;
    }

    public async Task<List<DeviceStatusDto>> Handle(
        GetDeviceStatusesQuery request,
        CancellationToken cancellationToken)
    {
        var statuses = await _context.DeviceStatuses
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

        var translations = await _translator.GetMapAsync(
            LookupTypes.DeviceStatus, cancellationToken);
        if (translations.Count == 0)
            return statuses;

        return statuses
            .Select(s => translations.TryGetValue(s.Id, out var tr)
                ? s with { Name = tr.Name, Description = tr.Description ?? s.Description }
                : s)
            .ToList();
    }
}
