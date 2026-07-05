using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Devices.Commands;

public record DeleteMaintenanceTypeCommand(Guid Id) : IRequest;

public class DeleteMaintenanceTypeCommandHandler
    : IRequestHandler<DeleteMaintenanceTypeCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DeleteMaintenanceTypeCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        DeleteMaintenanceTypeCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        // Exists at all (system or tenant-owned)?
        var maintenanceType = await _context.MaintenanceTypes
            .FirstOrDefaultAsync(x =>
                x.Id == request.Id &&
                !x.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException(nameof(MaintenanceType), request.Id);

        // System values cannot be deleted.
        if (maintenanceType.IsSystem)
            throw new ForbiddenAccessException();

        // Must belong to the current tenant.
        if (maintenanceType.TenantId != _currentUser.TenantId)
            throw new ForbiddenAccessException();

        maintenanceType.Deactivate();
        _context.Update(maintenanceType);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
