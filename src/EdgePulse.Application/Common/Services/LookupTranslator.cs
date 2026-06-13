using EdgePulse.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Common.Services;

public class LookupTranslator : ILookupTranslator
{
    private readonly IApplicationDbContext _context;
    private readonly ILocaleContext _locale;
    private readonly ICurrentUserService _currentUser;

    public LookupTranslator(
        IApplicationDbContext context,
        ILocaleContext locale,
        ICurrentUserService currentUser)
    {
        _context = context;
        _locale = locale;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyDictionary<Guid, LookupTranslationValue>> GetMapAsync(
        string lookupType,
        CancellationToken cancellationToken = default)
    {
        var locale = _locale.CurrentLocale;

        // Default locale needs no translation — keep stored English names.
        if (string.IsNullOrEmpty(locale) || locale == "en")
            return EmptyMap;

        var tenantId = _currentUser.TenantId;

        // Pull both global (TenantId == null) and tenant-specific rows for this
        // type+locale, then let tenant rows win.
        var rows = await _context.LookupTranslations
            .Where(x =>
                !x.IsDeleted &&
                x.LookupType == lookupType &&
                x.LocaleCode == locale &&
                (x.TenantId == null || x.TenantId == tenantId))
            .Select(x => new { x.ItemId, x.TenantId, x.Name, x.Description })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
            return EmptyMap;

        var map = new Dictionary<Guid, LookupTranslationValue>();
        // Global first, tenant-specific second so it overwrites.
        foreach (var r in rows.OrderBy(r => r.TenantId.HasValue ? 1 : 0))
            map[r.ItemId] = new LookupTranslationValue(r.Name, r.Description);

        return map;
    }

    private static readonly IReadOnlyDictionary<Guid, LookupTranslationValue> EmptyMap
        = new Dictionary<Guid, LookupTranslationValue>();
}
