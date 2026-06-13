using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Localization.Commands;

public record UiStringEntry(string Key, string? Value);

/// <summary>
/// Bulk create/update/clear UI string overrides for one locale. Used by CSV
/// import and "pre-fill from English". An entry with an empty value removes
/// the override (reverts to bundled JSON).
/// </summary>
public record BulkUpsertUiStringsCommand(
    string LocaleCode,
    IReadOnlyList<UiStringEntry> Entries
) : IRequest<int>;

public class BulkUpsertUiStringsCommandHandler
    : IRequestHandler<BulkUpsertUiStringsCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public BulkUpsertUiStringsCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(
        BulkUpsertUiStringsCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        var locale = request.LocaleCode.Trim().ToLowerInvariant();
        var tenantId = _currentUser.TenantId;

        var localeExists = await _context.Locales
            .AnyAsync(x => x.Code == locale && !x.IsDeleted, cancellationToken);
        if (!localeExists)
            throw new NotFoundException(nameof(Locale), locale);

        // Load existing tenant rows for this locale to update in place.
        var existing = await _context.UiStringTranslations
            .Where(x => !x.IsDeleted && x.LocaleCode == locale && x.TenantId == tenantId)
            .ToListAsync(cancellationToken);
        var byKey = existing.ToDictionary(x => x.Key, x => x);

        var affected = 0;
        foreach (var entry in request.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Key)) continue;
            var key = entry.Key.Trim();

            byKey.TryGetValue(key, out var row);

            if (string.IsNullOrWhiteSpace(entry.Value))
            {
                if (row != null)
                {
                    row.MarkAsDeleted();
                    _context.Update(row);
                    affected++;
                }
                continue;
            }

            if (row != null)
            {
                if (row.Value != entry.Value)
                {
                    row.Update(entry.Value);
                    _context.Update(row);
                    affected++;
                }
            }
            else
            {
                _context.Add(UiStringTranslation.Create(locale, key, entry.Value, tenantId));
                affected++;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return affected;
    }
}
