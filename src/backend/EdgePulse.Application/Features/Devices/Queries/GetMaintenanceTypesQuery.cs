using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Constants;
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
    private readonly ILookupTranslator _translator;

    public GetMaintenanceTypesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ILookupTranslator translator)
    {
        _context = context;
        _currentUser = currentUser;
        _translator = translator;
    }

    public async Task<List<MaintenanceTypeDto>> Handle(
        GetMaintenanceTypesQuery request,
        CancellationToken cancellationToken)
    {
        var maintenanceTypes = await _context.MaintenanceTypes
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

        var translations = await _translator.GetMapAsync(
            LookupTypes.MaintenanceType, cancellationToken);
        if (translations.Count == 0)
            return maintenanceTypes;

        return maintenanceTypes
            .Select(m => translations.TryGetValue(m.Id, out var tr)
                ? m with { Name = tr.Name, Description = tr.Description ?? m.Description }
                : m)
            .ToList();
    }
}
