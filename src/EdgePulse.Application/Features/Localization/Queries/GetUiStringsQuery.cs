using EdgePulse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Localization.Queries;

/// <summary>
/// Returns the DB-stored UI string overrides for a locale as a flat
/// key → value map. The frontend layers these on top of the bundled JSON.
/// Tenant-specific overrides win over global ones.
/// </summary>
public record GetUiStringsQuery(string LocaleCode) : IRequest<Dictionary<string, string>>;

public class GetUiStringsQueryHandler
    : IRequestHandler<GetUiStringsQuery, Dictionary<string, string>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetUiStringsQueryHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Dictionary<string, string>> Handle(
        GetUiStringsQuery request, CancellationToken cancellationToken)
    {
        var locale = request.LocaleCode.Trim().ToLowerInvariant();
        var tenantId = _currentUser.TenantId;

        var rows = await _context.UiStringTranslations
            .Where(x =>
                !x.IsDeleted &&
                x.LocaleCode == locale &&
                (x.TenantId == null || x.TenantId == tenantId))
            .Select(x => new { x.Key, x.Value, x.TenantId })
            .ToListAsync(cancellationToken);

        var map = new Dictionary<string, string>();
        foreach (var r in rows.OrderBy(r => r.TenantId.HasValue ? 1 : 0))
            map[r.Key] = r.Value;  // tenant overrides global

        return map;
    }
}
