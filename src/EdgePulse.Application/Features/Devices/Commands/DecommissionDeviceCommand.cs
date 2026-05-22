using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Constants;
using EdgePulse.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Devices.Commands;

public record DecommissionDeviceCommand(Guid DeviceId) : IRequest;

public class DecommissionDeviceCommandHandler
    : IRequestHandler<DecommissionDeviceCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DecommissionDeviceCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        DecommissionDeviceCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Role check — Operators and Executives cannot decommission
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        // 2. Existence check — query without tenant filter first
        var device = await _context.Devices
            .FirstOrDefaultAsync(x =>
                x.Id == request.DeviceId &&
                !x.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException(nameof(Device), request.DeviceId);

        // 3. Ownership check — device must belong to current tenant
        if (device.TenantId != _currentUser.TenantId)
            throw new ForbiddenAccessException();

        // 4. MillManager scope — can only decommission devices in their mill
        if (_currentUser.IsMillManager &&
            _currentUser.MillId.HasValue &&
            _currentUser.MillId.Value != device.MillId)
            throw new ForbiddenAccessException();

        // 5. Decommission device: status → Decommissioned, IsDeleted = true (soft delete)
        device.Decommission(GenericDeviceStatusIds.Decommissioned);
        _context.Update(device);

        // 6. Revoke all active API keys for this device
        var activeApiKeys = await _context.DeviceApiKeys
            .Where(x =>
                x.DeviceId == device.Id &&
                x.IsActive &&
                !x.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var apiKey in activeApiKeys)
        {
            apiKey.Revoke("Device decommissioned");
            _context.Update(apiKey);
        }

        // 7. Persist all changes in a single transaction
        await _context.SaveChangesAsync(cancellationToken);
    }
}
