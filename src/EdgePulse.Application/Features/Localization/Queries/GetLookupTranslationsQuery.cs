using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Localization.Queries;

/// <summary>
/// Returns every lookup item of a given type alongside its translation (if any)
/// in the requested locale — driving the translation editor grid. The English
/// source name is always included so the editor can show "original → translation".
/// </summary>
public record GetLookupTranslationsQuery(string LookupType, string LocaleCode)
    : IRequest<List<LookupTranslationRowDto>>;

public record LookupTranslationRowDto(
    Guid ItemId,
    string SourceName,           // stored English name
    string? SourceDescription,
    string? TranslatedName,      // null if not yet translated
    string? TranslatedDescription
);

public class GetLookupTranslationsQueryHandler
    : IRequestHandler<GetLookupTranslationsQuery, List<LookupTranslationRowDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetLookupTranslationsQueryHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<LookupTranslationRowDto>> Handle(
        GetLookupTranslationsQuery request, CancellationToken cancellationToken)
    {
        if (!LookupTypes.IsTranslatable(request.LookupType))
            throw new ConflictException($"Lookup type '{request.LookupType}' is not translatable.");

        var locale = request.LocaleCode.Trim().ToLowerInvariant();

        // Source items (id, name, description) for the requested lookup type.
        var items = await GetSourceItemsAsync(request.LookupType, cancellationToken);

        var tenantId = _currentUser.TenantId;
        var translations = await _context.LookupTranslations
            .Where(x =>
                !x.IsDeleted &&
                x.LookupType == request.LookupType &&
                x.LocaleCode == locale &&
                (x.TenantId == null || x.TenantId == tenantId))
            .ToListAsync(cancellationToken);

        // Tenant-specific translations take precedence over global ones.
        var byItem = translations
            .OrderBy(x => x.TenantId.HasValue ? 1 : 0)
            .GroupBy(x => x.ItemId)
            .ToDictionary(g => g.Key, g => g.Last());

        return items
            .Select(it =>
            {
                byItem.TryGetValue(it.Id, out var tr);
                return new LookupTranslationRowDto(
                    it.Id, it.Name, it.Description,
                    tr?.Name, tr?.Description);
            })
            .ToList();
    }

    private async Task<List<SourceItem>> GetSourceItemsAsync(
        string lookupType, CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId;

        // Each lookup table is tenant-scoped the same way (TenantId null = global).
        return lookupType switch
        {
            LookupTypes.DeviceType => await _context.DeviceTypes
                .Where(x => !x.IsDeleted && (x.TenantId == null || x.TenantId == tenantId))
                .OrderBy(x => x.SortOrder)
                .Select(x => new SourceItem(x.Id, x.Name, x.Description)).ToListAsync(ct),

            LookupTypes.DeviceStatus => await _context.DeviceStatuses
                .Where(x => !x.IsDeleted && (x.TenantId == null || x.TenantId == tenantId))
                .OrderBy(x => x.SortOrder)
                .Select(x => new SourceItem(x.Id, x.Name, x.Description)).ToListAsync(ct),

            LookupTypes.LocationType => await _context.LocationTypes
                .Where(x => !x.IsDeleted && (x.TenantId == null || x.TenantId == tenantId))
                .OrderBy(x => x.SortOrder)
                .Select(x => new SourceItem(x.Id, x.Name, x.Description)).ToListAsync(ct),

            LookupTypes.MaintenanceType => await _context.MaintenanceTypes
                .Where(x => !x.IsDeleted && (x.TenantId == null || x.TenantId == tenantId))
                .OrderBy(x => x.SortOrder)
                .Select(x => new SourceItem(x.Id, x.Name, x.Description)).ToListAsync(ct),

            LookupTypes.MetricType => await _context.MetricTypes
                .Where(x => !x.IsDeleted && (x.TenantId == null || x.TenantId == tenantId))
                .OrderBy(x => x.SortOrder)
                .Select(x => new SourceItem(x.Id, x.Name, x.Description)).ToListAsync(ct),

            LookupTypes.AlertSeverity => await _context.AlertSeverities
                .Where(x => !x.IsDeleted && (x.TenantId == null || x.TenantId == tenantId))
                .OrderBy(x => x.SortOrder)
                .Select(x => new SourceItem(x.Id, x.Name, x.Description)).ToListAsync(ct),

            _ => new List<SourceItem>(),
        };
    }

    private record SourceItem(Guid Id, string Name, string? Description);
}
