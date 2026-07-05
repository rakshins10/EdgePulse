using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Localization.Commands;

public record SetDefaultLocaleCommand(Guid Id) : IRequest;

public class SetDefaultLocaleCommandHandler : IRequestHandler<SetDefaultLocaleCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SetDefaultLocaleCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(SetDefaultLocaleCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsSuperAdmin && !_currentUser.IsCustomerAdmin)
            throw new ForbiddenAccessException();

        var target = await _context.Locales
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Locale), request.Id);

        // Clear the current default(s), then set the new one.
        var currentDefaults = await _context.Locales
            .Where(x => x.IsDefault && !x.IsDeleted && x.Id != target.Id)
            .ToListAsync(cancellationToken);
        foreach (var loc in currentDefaults)
        {
            loc.ClearDefault();
            _context.Update(loc);
        }

        target.SetAsDefault();
        _context.Update(target);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
