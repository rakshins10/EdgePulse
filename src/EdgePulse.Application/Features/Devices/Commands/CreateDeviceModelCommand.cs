using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Devices.Commands;

public record CreateDeviceModelCommand(
    Guid ManufacturerId,
    string Name,
    string Code,
    string? ModelNumber,
    string? Specifications,
    int SortOrder = 0
) : IRequest<Guid>;

public class CreateDeviceModelCommandValidator
    : AbstractValidator<CreateDeviceModelCommand>
{
    public CreateDeviceModelCommandValidator()
    {
        RuleFor(x => x.ManufacturerId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50)
            .Matches("^[A-Z0-9_]+$").WithMessage(
                "Code must be uppercase letters, numbers and underscores only.");
        RuleFor(x => x.ModelNumber).MaximumLength(100)
            .When(x => x.ModelNumber != null);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public class CreateDeviceModelCommandHandler
    : IRequestHandler<CreateDeviceModelCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateDeviceModelCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(
        CreateDeviceModelCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        // Verify manufacturer exists and belongs to tenant
        var manufacturerExists = await _context.DeviceManufacturers
            .AnyAsync(x =>
                x.Id == request.ManufacturerId &&
                (x.TenantId == null ||
                 x.TenantId == _currentUser.TenantId) &&
                !x.IsDeleted, cancellationToken);

        if (!manufacturerExists)
            throw new NotFoundException(
                nameof(DeviceManufacturer), request.ManufacturerId);

        var exists = await _context.DeviceModels
            .AnyAsync(x =>
                x.TenantId == _currentUser.TenantId &&
                x.ManufacturerId == request.ManufacturerId &&
                x.Code == request.Code.ToUpperInvariant() &&
                !x.IsDeleted, cancellationToken);

        if (exists)
            throw new ConflictException(
                $"Device model with code '{request.Code}' already exists " +
                $"for this manufacturer.");

        var model = DeviceModel.CreateCustomValue(
            tenantId: _currentUser.TenantId,
            manufacturerId: request.ManufacturerId,
            name: request.Name,
            code: request.Code,
            modelNumber: request.ModelNumber,
            specifications: request.Specifications,
            sortOrder: request.SortOrder);

        _context.Add(model);
        await _context.SaveChangesAsync(cancellationToken);
        return model.Id;
    }
}
