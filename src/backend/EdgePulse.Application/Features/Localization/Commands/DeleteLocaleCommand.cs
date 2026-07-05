using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Localization.Commands;

public record DeleteLocaleCommand(Guid Id) : IRequest;

public class DeleteLocaleCommandHandler : IRequestHandler<DeleteLocaleCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DeleteLocaleCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteLocaleCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsSuperAdmin && !_currentUser.IsCustomerAdmin)
            throw new ForbiddenAccessException();

        var locale = await _context.Locales
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Locale), request.Id);

        if (locale.IsDefault)
            throw new ConflictException(
                "The default locale cannot be deleted. Set another locale as default first.");

        // Soft-delete the locale and its translations so they stop resolving.
        var translations = await _context.LookupTranslations
            .Where(x => x.LocaleCode == locale.Code && !x.IsDeleted)
            .ToListAsync(cancellationToken);
        foreach (var tr in translations)
        {
            tr.MarkAsDeleted();
            _context.Update(tr);
        }

        locale.MarkAsDeleted();
        _context.Update(locale);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
