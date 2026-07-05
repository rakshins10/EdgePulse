using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Alerts.Commands;

public record DeleteAlertThresholdCommand(Guid Id) : IRequest;

public class DeleteAlertThresholdCommandHandler
    : IRequestHandler<DeleteAlertThresholdCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DeleteAlertThresholdCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        DeleteAlertThresholdCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        var threshold = await _context.AlertThresholds
            .FirstOrDefaultAsync(x =>
                x.Id == request.Id &&
                x.TenantId == _currentUser.TenantId &&
                !x.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException(nameof(AlertThreshold), request.Id);

        // MillManager scope check
        if (_currentUser.IsMillManager)
        {
            var device = await _context.Devices
                .FirstOrDefaultAsync(x => x.Id == threshold.DeviceId, cancellationToken);
            if (device?.MillId != _currentUser.MillId)
                throw new ForbiddenAccessException();
        }

        // Soft delete — threshold history preserved for historical alerts
        threshold.MarkAsDeleted();
        threshold.Deactivate();
        _context.Update(threshold);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
