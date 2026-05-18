using EdgePulse.Domain.Entities;
using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Devices.Commands;

public record DeleteDeviceTypeCommand(Guid Id) : IRequest;

public class DeleteDeviceTypeCommandHandler
    : IRequestHandler<DeleteDeviceTypeCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DeleteDeviceTypeCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        DeleteDeviceTypeCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        var deviceType = await _context.DeviceTypes
            .FirstOrDefaultAsync(x =>
                x.Id == request.Id &&
                x.TenantId == _currentUser.TenantId &&
                !x.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException(nameof(DeviceType), request.Id);

        // System values cannot be deleted
        // Use TenantLookupOverride to deactivate them
        if (deviceType.IsSystem)
            throw new ForbiddenAccessException();

        // Check if any devices are using this type
        var inUse = await _context.Devices
            .AnyAsync(x =>
                x.TypeId == request.Id &&
                !x.IsDeleted,
                cancellationToken);

        if (inUse)
            throw new ConflictException(
                "Cannot delete device type that is assigned to active devices.");

        deviceType.Deactivate();
        _context.Update(deviceType);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
