using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Constants;
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
    private readonly ILookupTranslator _translator;

    public GetDeviceTypesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ILookupTranslator translator)
    {
        _context = context;
        _currentUser = currentUser;
        _translator = translator;
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

        var translations = await _translator.GetMapAsync(
            LookupTypes.DeviceType, cancellationToken);
        if (translations.Count == 0)
            return deviceTypes;

        return deviceTypes
            .Select(d => translations.TryGetValue(d.Id, out var tr)
                ? d with { Name = tr.Name, Description = tr.Description ?? d.Description }
                : d)
            .ToList();
    }
}
