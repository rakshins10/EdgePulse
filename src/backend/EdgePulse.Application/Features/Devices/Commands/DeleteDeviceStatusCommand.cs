using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Devices.Commands;

public record DeleteDeviceStatusCommand(Guid Id) : IRequest;

public class DeleteDeviceStatusCommandHandler
    : IRequestHandler<DeleteDeviceStatusCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DeleteDeviceStatusCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        DeleteDeviceStatusCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        // First check if it exists at all (system or tenant)
        var status = await _context.DeviceStatuses
            .FirstOrDefaultAsync(x =>
                x.Id == request.Id &&
                !x.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException(
                nameof(DeviceStatus), request.Id);

        // System values cannot be deleted by anyone
        // Tenant must use TenantLookupOverride to disable them
        if (status.IsSystem)
            throw new ForbiddenAccessException();

        // Tenant-owned type -- check it belongs to current tenant
        if (status.TenantId != _currentUser.TenantId)
            throw new ForbiddenAccessException();

        // Check if any devices are using this status
        var inUse = await _context.Devices
            .AnyAsync(x =>
                x.StatusId == request.Id &&
                !x.IsDeleted,
                cancellationToken);

        if (inUse)
            throw new ConflictException(
                "Cannot delete device status assigned to active devices.");

        status.Deactivate();
        _context.Update(status);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
