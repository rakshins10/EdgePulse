using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Devices.Commands;

/// <summary>
/// Edit a device's mutable details. Mill is intentionally NOT changeable
/// — to move a device between mills, decommission and re-register. Area
/// changes within the same mill are permitted.
/// </summary>
public record UpdateDeviceCommand(
    Guid Id,
    string Name,
    Guid AreaId,
    Guid TypeId,
    Guid? ManufacturerId,
    Guid? ModelId,
    string? SerialNumber,
    DateOnly? InstallDate,
    string? Description
) : IRequest;

public class UpdateDeviceCommandValidator : AbstractValidator<UpdateDeviceCommand>
{
    public UpdateDeviceCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.AreaId).NotEmpty();
        RuleFor(x => x.TypeId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SerialNumber).MaximumLength(100).When(x => x.SerialNumber != null);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description != null);
    }
}

public class UpdateDeviceCommandHandler : IRequestHandler<UpdateDeviceCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateDeviceCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateDeviceCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        var device = await _context.Devices
            .FirstOrDefaultAsync(x =>
                x.Id == request.Id &&
                x.TenantId == _currentUser.TenantId &&
                !x.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException(nameof(Device), request.Id);

        // MillManager can only edit devices in their mill
        if (_currentUser.IsMillManager &&
            _currentUser.MillId.HasValue &&
            _currentUser.MillId.Value != device.MillId)
            throw new ForbiddenAccessException();

        // Verify new area exists, belongs to same mill, is in same tenant
        var area = await _context.Areas
            .FirstOrDefaultAsync(a =>
                a.Id == request.AreaId &&
                a.TenantId == _currentUser.TenantId &&
                !a.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException(nameof(Area), request.AreaId);

        if (area.MillId != device.MillId)
            throw new ConflictException(
                "Area belongs to a different mill. To move a device across " +
                "mills, decommission and re-register it.");

        device.UpdateDetails(
            name: request.Name,
            areaId: request.AreaId,
            typeId: request.TypeId,
            manufacturerId: request.ManufacturerId,
            modelId: request.ModelId,
            serialNumber: request.SerialNumber,
            installDate: request.InstallDate,
            description: request.Description);

        _context.Update(device);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
