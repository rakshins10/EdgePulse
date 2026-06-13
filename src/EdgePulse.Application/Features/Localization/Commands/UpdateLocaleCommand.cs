using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Localization.Commands;

public record UpdateLocaleCommand(
    Guid Id,
    string DisplayName,
    string NativeName,
    string? Flag,
    bool IsEnabled,
    int SortOrder
) : IRequest;

public class UpdateLocaleCommandValidator : AbstractValidator<UpdateLocaleCommand>
{
    public UpdateLocaleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.NativeName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Flag).MaximumLength(20).When(x => x.Flag != null);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public class UpdateLocaleCommandHandler : IRequestHandler<UpdateLocaleCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateLocaleCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateLocaleCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsSuperAdmin && !_currentUser.IsCustomerAdmin)
            throw new ForbiddenAccessException();

        var locale = await _context.Locales
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Locale), request.Id);

        // The default locale cannot be disabled.
        var enabled = locale.IsDefault || request.IsEnabled;

        locale.UpdateDetails(
            request.DisplayName, request.NativeName, request.Flag, enabled, request.SortOrder);
        _context.Update(locale);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
