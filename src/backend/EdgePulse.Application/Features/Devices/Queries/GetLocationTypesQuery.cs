using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Constants;
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
    private readonly ILookupTranslator _translator;

    public GetLocationTypesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ILookupTranslator translator)
    {
        _context = context;
        _currentUser = currentUser;
        _translator = translator;
    }

    public async Task<List<LocationTypeDto>> Handle(
        GetLocationTypesQuery request,
        CancellationToken cancellationToken)
    {
        var locationTypes = await _context.LocationTypes
            .Where(x => !x.IsDeleted && x.IsActive)
            .Where(x => x.TenantId == null ||
                        x.TenantId == _currentUser.TenantId)
            .OrderBy(x => x.IsSystem ? 0 : 1)
            .ThenBy(x => x.SortOrder)
            .Select(x => new LocationTypeDto(
                x.Id, x.Name, x.Code,
                x.Description, x.IsSystem, x.SortOrder))
            .ToListAsync(cancellationToken);

        var translations = await _translator.GetMapAsync(
            LookupTypes.LocationType, cancellationToken);
        if (translations.Count == 0)
            return locationTypes;

        return locationTypes
            .Select(l => translations.TryGetValue(l.Id, out var tr)
                ? l with { Name = tr.Name, Description = tr.Description ?? l.Description }
                : l)
            .ToList();
    }
}
