using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Localization.Queries;

/// <summary>
/// Returns every translatable lookup item across all types with its English
/// source name. Drives CSV export (the Source column) and "pre-fill from
/// English" for new locales.
/// </summary>
public record GetLookupSourceItemsQuery : IRequest<List<LookupSourceItemDto>>;

public record LookupSourceItemDto(string LookupType, Guid ItemId, string SourceName);

public class GetLookupSourceItemsQueryHandler
    : IRequestHandler<GetLookupSourceItemsQuery, List<LookupSourceItemDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetLookupSourceItemsQueryHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<LookupSourceItemDto>> Handle(
        GetLookupSourceItemsQuery request, CancellationToken cancellationToken)
    {
        var t = _currentUser.TenantId;
        var result = new List<LookupSourceItemDto>();

        result.AddRange(await _context.DeviceTypes
            .Where(x => !x.IsDeleted && (x.TenantId == null || x.TenantId == t))
            .OrderBy(x => x.SortOrder)
            .Select(x => new LookupSourceItemDto(LookupTypes.DeviceType, x.Id, x.Name))
            .ToListAsync(cancellationToken));

        result.AddRange(await _context.DeviceStatuses
            .Where(x => !x.IsDeleted && (x.TenantId == null || x.TenantId == t))
            .OrderBy(x => x.SortOrder)
            .Select(x => new LookupSourceItemDto(LookupTypes.DeviceStatus, x.Id, x.Name))
            .ToListAsync(cancellationToken));

        result.AddRange(await _context.LocationTypes
            .Where(x => !x.IsDeleted && (x.TenantId == null || x.TenantId == t))
            .OrderBy(x => x.SortOrder)
            .Select(x => new LookupSourceItemDto(LookupTypes.LocationType, x.Id, x.Name))
            .ToListAsync(cancellationToken));

        result.AddRange(await _context.MaintenanceTypes
            .Where(x => !x.IsDeleted && (x.TenantId == null || x.TenantId == t))
            .OrderBy(x => x.SortOrder)
            .Select(x => new LookupSourceItemDto(LookupTypes.MaintenanceType, x.Id, x.Name))
            .ToListAsync(cancellationToken));

        result.AddRange(await _context.MetricTypes
            .Where(x => !x.IsDeleted && (x.TenantId == null || x.TenantId == t))
            .OrderBy(x => x.SortOrder)
            .Select(x => new LookupSourceItemDto(LookupTypes.MetricType, x.Id, x.Name))
            .ToListAsync(cancellationToken));

        result.AddRange(await _context.AlertSeverities
            .Where(x => !x.IsDeleted && (x.TenantId == null || x.TenantId == t))
            .OrderBy(x => x.SortOrder)
            .Select(x => new LookupSourceItemDto(LookupTypes.AlertSeverity, x.Id, x.Name))
            .ToListAsync(cancellationToken));

        return result;
    }
}
