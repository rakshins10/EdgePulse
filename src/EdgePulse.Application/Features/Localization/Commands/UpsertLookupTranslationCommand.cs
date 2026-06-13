using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Constants;
using EdgePulse.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Localization.Commands;

/// <summary>
/// Creates or updates a tenant-scoped translation for one lookup item in one
/// locale. An empty Name removes the translation (revert to source name).
/// </summary>
public record UpsertLookupTranslationCommand(
    string LookupType,
    Guid ItemId,
    string LocaleCode,
    string? Name,
    string? Description
) : IRequest;

public class UpsertLookupTranslationCommandValidator
    : AbstractValidator<UpsertLookupTranslationCommand>
{
    public UpsertLookupTranslationCommandValidator()
    {
        RuleFor(x => x.LookupType).NotEmpty();
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.LocaleCode).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Name).MaximumLength(200).When(x => x.Name != null);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description != null);
    }
}

public class UpsertLookupTranslationCommandHandler
    : IRequestHandler<UpsertLookupTranslationCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpsertLookupTranslationCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        UpsertLookupTranslationCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        if (!LookupTypes.IsTranslatable(request.LookupType))
            throw new ConflictException($"Lookup type '{request.LookupType}' is not translatable.");

        var locale = request.LocaleCode.Trim().ToLowerInvariant();
        var tenantId = _currentUser.TenantId;

        var localeExists = await _context.Locales
            .AnyAsync(x => x.Code == locale && !x.IsDeleted, cancellationToken);
        if (!localeExists)
            throw new NotFoundException(nameof(Locale), locale);

        var existing = await _context.LookupTranslations
            .FirstOrDefaultAsync(x =>
                !x.IsDeleted &&
                x.LookupType == request.LookupType &&
                x.ItemId == request.ItemId &&
                x.LocaleCode == locale &&
                x.TenantId == tenantId,
                cancellationToken);

        // Empty name => remove any existing translation (revert to source).
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            if (existing != null)
            {
                existing.MarkAsDeleted();
                _context.Update(existing);
                await _context.SaveChangesAsync(cancellationToken);
            }
            return;
        }

        if (existing != null)
        {
            existing.Update(request.Name.Trim(), request.Description);
            _context.Update(existing);
        }
        else
        {
            var translation = LookupTranslation.Create(
                request.LookupType, request.ItemId, locale,
                request.Name.Trim(), request.Description, tenantId);
            _context.Add(translation);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
