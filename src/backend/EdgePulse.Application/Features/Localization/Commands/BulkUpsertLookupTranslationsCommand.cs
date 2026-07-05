using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Constants;
using EdgePulse.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Localization.Commands;

public record LookupTranslationEntry(string LookupType, Guid ItemId, string? Name);

/// <summary>
/// Bulk create/update/clear lookup item translations for one locale. Used by
/// CSV import and "pre-fill from English". Empty name removes the translation.
/// </summary>
public record BulkUpsertLookupTranslationsCommand(
    string LocaleCode,
    IReadOnlyList<LookupTranslationEntry> Entries
) : IRequest<int>;

public class BulkUpsertLookupTranslationsCommandHandler
    : IRequestHandler<BulkUpsertLookupTranslationsCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public BulkUpsertLookupTranslationsCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(
        BulkUpsertLookupTranslationsCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        var locale = request.LocaleCode.Trim().ToLowerInvariant();
        var tenantId = _currentUser.TenantId;

        var localeExists = await _context.Locales
            .AnyAsync(x => x.Code == locale && !x.IsDeleted, cancellationToken);
        if (!localeExists)
            throw new NotFoundException(nameof(Locale), locale);

        var existing = await _context.LookupTranslations
            .Where(x => !x.IsDeleted && x.LocaleCode == locale && x.TenantId == tenantId)
            .ToListAsync(cancellationToken);
        var byItem = existing.ToDictionary(x => x.ItemId, x => x);

        var affected = 0;
        foreach (var entry in request.Entries)
        {
            if (!LookupTypes.IsTranslatable(entry.LookupType) || entry.ItemId == Guid.Empty)
                continue;

            byItem.TryGetValue(entry.ItemId, out var row);

            if (string.IsNullOrWhiteSpace(entry.Name))
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
                if (row.Name != entry.Name)
                {
                    row.Update(entry.Name.Trim(), row.Description);
                    _context.Update(row);
                    affected++;
                }
            }
            else
            {
                _context.Add(LookupTranslation.Create(
                    entry.LookupType, entry.ItemId, locale, entry.Name.Trim(), null, tenantId));
                affected++;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return affected;
    }
}
