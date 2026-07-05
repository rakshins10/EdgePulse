using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Devices.Commands;

public record CreateDeviceManufacturerCommand(
    string Name,
    string Code,
    string? Website,
    string? Country,
    int SortOrder = 0
) : IRequest<Guid>;

public class CreateDeviceManufacturerCommandValidator
    : AbstractValidator<CreateDeviceManufacturerCommand>
{
    public CreateDeviceManufacturerCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50)
            .Matches("^[A-Z0-9_]+$").WithMessage(
                "Code must be uppercase letters, numbers and underscores only.");
        RuleFor(x => x.Website).MaximumLength(300)
            .When(x => x.Website != null);
        RuleFor(x => x.Country).MaximumLength(100)
            .When(x => x.Country != null);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public class CreateDeviceManufacturerCommandHandler
    : IRequestHandler<CreateDeviceManufacturerCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateDeviceManufacturerCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(
        CreateDeviceManufacturerCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        var exists = await _context.DeviceManufacturers
            .AnyAsync(x =>
                x.TenantId == _currentUser.TenantId &&
                x.Code == request.Code.ToUpperInvariant() &&
                !x.IsDeleted, cancellationToken);

        if (exists)
            throw new ConflictException(
                $"Manufacturer with code '{request.Code}' already exists.");

        var manufacturer = DeviceManufacturer.CreateCustomValue(
            tenantId: _currentUser.TenantId,
            name: request.Name,
            code: request.Code,
            website: request.Website,
            country: request.Country,
            sortOrder: request.SortOrder);

        _context.Add(manufacturer);
        await _context.SaveChangesAsync(cancellationToken);
        return manufacturer.Id;
    }
}
