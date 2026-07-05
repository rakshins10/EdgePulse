using EdgePulse.Domain.Entities;
using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Devices.Commands;

public record UpdateDeviceTypeCommand(
    Guid Id,
    string Name,
    string? Description,
    string? Icon,
    int SortOrder
) : IRequest;

public class UpdateDeviceTypeCommandValidator
    : AbstractValidator<UpdateDeviceTypeCommand>
{
    public UpdateDeviceTypeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(300).When(x => x.Description != null);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0);
    }
}

public class UpdateDeviceTypeCommandHandler
    : IRequestHandler<UpdateDeviceTypeCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateDeviceTypeCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        UpdateDeviceTypeCommand request,
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

        // System values cannot be edited directly
        // Tenant must use TenantLookupOverride to rename them
        if (deviceType.IsSystem)
            throw new ForbiddenAccessException();

        deviceType.UpdateDetails(request.Name, request.Description);
        _context.Update(deviceType);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
