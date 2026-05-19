using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Alerts.Commands;

public record UpdateAlertSeverityCommand(
    Guid Id,
    string Name,
    string? Description,
    string? Color,
    int Priority,
    int SortOrder
) : IRequest;

public class UpdateAlertSeverityCommandValidator
    : AbstractValidator<UpdateAlertSeverityCommand>
{
    public UpdateAlertSeverityCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description)
            .MaximumLength(300).When(x => x.Description != null);
        RuleFor(x => x.Color)
            .Matches("^#[0-9A-Fa-f]{6}$").When(x => x.Color != null)
            .WithMessage("Color must be a valid hex code e.g. #ef4444");
        RuleFor(x => x.Priority)
            .GreaterThan(0);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public class UpdateAlertSeverityCommandHandler
    : IRequestHandler<UpdateAlertSeverityCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateAlertSeverityCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        UpdateAlertSeverityCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        var severity = await _context.AlertSeverities
            .FirstOrDefaultAsync(x =>
                x.Id == request.Id &&
                !x.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException(
                nameof(AlertSeverity), request.Id);

        if (severity.IsSystem)
            throw new ForbiddenAccessException();

        if (severity.TenantId != _currentUser.TenantId)
            throw new ForbiddenAccessException();

        severity.UpdateDetails(request.Name, request.Description);
        _context.Update(severity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
