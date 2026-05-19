using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Alerts.Commands;

public record CreateAlertSeverityCommand(
    string Name,
    string Code,
    string? Description,
    string? Color,
    int Priority,
    int SortOrder = 0
) : IRequest<Guid>;

public class CreateAlertSeverityCommandValidator
    : AbstractValidator<CreateAlertSeverityCommand>
{
    public CreateAlertSeverityCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.")
            .MaximumLength(50)
            .Matches("^[A-Z0-9_]+$").WithMessage(
                "Code must be uppercase letters, numbers and underscores only.");

        RuleFor(x => x.Description)
            .MaximumLength(300).When(x => x.Description != null);

        RuleFor(x => x.Color)
            .Matches("^#[0-9A-Fa-f]{6}$").When(x => x.Color != null)
            .WithMessage("Color must be a valid hex code e.g. #ef4444");

        RuleFor(x => x.Priority)
            .GreaterThan(0).WithMessage("Priority must be greater than 0.");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0);
    }
}

public class CreateAlertSeverityCommandHandler
    : IRequestHandler<CreateAlertSeverityCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateAlertSeverityCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(
        CreateAlertSeverityCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        var exists = await _context.AlertSeverities
            .AnyAsync(x =>
                x.TenantId == _currentUser.TenantId &&
                x.Code == request.Code.ToUpperInvariant() &&
                !x.IsDeleted,
                cancellationToken);

        if (exists)
            throw new ConflictException(
                $"Alert severity with code '{request.Code}' already exists.");

        var severity = AlertSeverity.CreateCustomValue(
            tenantId: _currentUser.TenantId,
            name: request.Name,
            code: request.Code,
            priority: request.Priority,
            color: request.Color,
            description: request.Description,
            sortOrder: request.SortOrder);

        _context.Add(severity);
        await _context.SaveChangesAsync(cancellationToken);

        return severity.Id;
    }
}
