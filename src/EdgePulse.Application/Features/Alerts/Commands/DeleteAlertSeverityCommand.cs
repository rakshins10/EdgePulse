using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Alerts.Commands;

public record DeleteAlertSeverityCommand(Guid Id) : IRequest;

public class DeleteAlertSeverityCommandHandler
    : IRequestHandler<DeleteAlertSeverityCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DeleteAlertSeverityCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        DeleteAlertSeverityCommand request,
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

        severity.Deactivate();
        _context.Update(severity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
