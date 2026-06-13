using EdgePulse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Localization.Queries;

/// <summary>List locales. When EnabledOnly is true, returns only enabled ones
/// (used by the language switcher). Otherwise returns all (admin management).</summary>
public record GetLocalesQuery(bool EnabledOnly = false) : IRequest<List<LocaleDto>>;

public record LocaleDto(
    Guid Id,
    string Code,
    string DisplayName,
    string NativeName,
    string? Flag,
    bool IsEnabled,
    bool IsDefault,
    int SortOrder
);

public class GetLocalesQueryHandler : IRequestHandler<GetLocalesQuery, List<LocaleDto>>
{
    private readonly IApplicationDbContext _context;

    public GetLocalesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<LocaleDto>> Handle(
        GetLocalesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Locales.Where(x => !x.IsDeleted);
        if (request.EnabledOnly)
            query = query.Where(x => x.IsEnabled);

        return await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayName)
            .Select(x => new LocaleDto(
                x.Id, x.Code, x.DisplayName, x.NativeName,
                x.Flag, x.IsEnabled, x.IsDefault, x.SortOrder))
            .ToListAsync(cancellationToken);
    }
}
