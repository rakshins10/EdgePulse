using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Localization.Commands;

public record CreateLocaleCommand(
    string Code,
    string DisplayName,
    string NativeName,
    string? Flag,
    bool IsEnabled,
    int SortOrder
) : IRequest<Guid>;

public class CreateLocaleCommandValidator : AbstractValidator<CreateLocaleCommand>
{
    public CreateLocaleCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().MaximumLength(10)
            .Matches("^[a-zA-Z]{2,3}(-[a-zA-Z0-9]{2,8})?$")
            .WithMessage("Code must be a valid language tag, e.g. 'en', 'fi', 'pt-BR'.");
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.NativeName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Flag).MaximumLength(20).When(x => x.Flag != null);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public class CreateLocaleCommandHandler : IRequestHandler<CreateLocaleCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateLocaleCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateLocaleCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsSuperAdmin && !_currentUser.IsCustomerAdmin)
            throw new ForbiddenAccessException();

        var code = request.Code.Trim().ToLowerInvariant();
        var exists = await _context.Locales
            .AnyAsync(x => x.Code == code && !x.IsDeleted, cancellationToken);
        if (exists)
            throw new ConflictException($"Locale '{code}' already exists.");

        var locale = Locale.Create(
            code, request.DisplayName, request.NativeName,
            request.Flag, request.IsEnabled, isDefault: false, request.SortOrder);

        _context.Add(locale);
        await _context.SaveChangesAsync(cancellationToken);
        return locale.Id;
    }
}
