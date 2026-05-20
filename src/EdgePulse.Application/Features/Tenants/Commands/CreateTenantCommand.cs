using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Tenants.Commands;

public record CreateTenantCommand(
    string Name,
    string Slug,
    string ContactEmail,
    Guid? TemplateId
) : IRequest<Guid>;

public class CreateTenantCommandValidator
    : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .MaximumLength(100)
            .Matches("^[a-z0-9-]+$").WithMessage(
                "Slug must be lowercase letters, numbers and hyphens only.");

        RuleFor(x => x.ContactEmail)
            .NotEmpty().WithMessage("Contact email is required.")
            .EmailAddress().WithMessage("Invalid email address.")
            .MaximumLength(300);
    }
}

public class CreateTenantCommandHandler
    : IRequestHandler<CreateTenantCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateTenantCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(
        CreateTenantCommand request,
        CancellationToken cancellationToken)
    {
        // Only SuperAdmin can create tenants
        if (!_currentUser.IsSuperAdmin)
            throw new ForbiddenAccessException();

        // Check slug is unique
        var slugExists = await _context.Tenants
            .AnyAsync(x =>
                x.Slug == request.Slug.ToLowerInvariant() &&
                !x.IsDeleted,
                cancellationToken);

        if (slugExists)
            throw new ConflictException(
                $"Tenant with slug '{request.Slug}' already exists.");

        var tenant = Tenant.Create(
            name: request.Name,
            slug: request.Slug,
            contactEmail: request.ContactEmail);

        _context.Add(tenant);

        // Assign industry template if provided
        if (request.TemplateId.HasValue)
        {
            var templateExists = await _context.IndustryTemplates
                .AnyAsync(x =>
                    x.Id == request.TemplateId.Value &&
                    !x.IsDeleted,
                    cancellationToken);

            if (!templateExists)
                throw new NotFoundException(
                    nameof(IndustryTemplate), request.TemplateId.Value);

            var tenantTemplate = TenantTemplate.Create(
                tenantId: tenant.Id,
                templateId: request.TemplateId.Value,
                assignedBy: _currentUser.UserId);

            _context.Add(tenantTemplate);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return tenant.Id;
    }
}
