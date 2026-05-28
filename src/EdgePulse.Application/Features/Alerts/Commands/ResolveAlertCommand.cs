using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Alerts.Commands;

public record ResolveAlertCommand(
    Guid Id,
    string? Notes
) : IRequest;

public class ResolveAlertCommandValidator
    : AbstractValidator<ResolveAlertCommand>
{
    public ResolveAlertCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Notes)
            .MaximumLength(1000).When(x => x.Notes != null);
    }
}

public class ResolveAlertCommandHandler
    : IRequestHandler<ResolveAlertCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ResolveAlertCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        ResolveAlertCommand request,
        CancellationToken cancellationToken)
    {
        // Executives cannot resolve alerts
        if (_currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        var alert = await _context.Alerts
            .FirstOrDefaultAsync(x =>
                x.Id == request.Id &&
                x.TenantId == _currentUser.TenantId,
                cancellationToken)
            ?? throw new NotFoundException(nameof(Alert), request.Id);

        // MillManager can only resolve alerts in their mill
        if (_currentUser.IsMillManager &&
            _currentUser.MillId != alert.MillId)
            throw new ForbiddenAccessException();

        // Operator can only resolve alerts in their assigned areas
        if (_currentUser.IsOperator &&
            _currentUser.AreaIds.Any() &&
            !_currentUser.AreaIds.Contains(alert.AreaId))
            throw new ForbiddenAccessException();

        alert.Resolve(
            resolvedBy: _currentUser.Email,
            notes: request.Notes);

        _context.Update(alert);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
