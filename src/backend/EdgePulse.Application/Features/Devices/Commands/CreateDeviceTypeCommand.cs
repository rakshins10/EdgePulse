using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Constants;
using EdgePulse.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Devices.Commands;

public record CreateDeviceTypeCommand(
    string Name,
    string Code,
    string? Description,
    string? Icon,
    int SortOrder = 0
) : IRequest<Guid>;

public class CreateDeviceTypeCommandValidator
    : AbstractValidator<CreateDeviceTypeCommand>
{
    public CreateDeviceTypeCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.")
            .MaximumLength(50).WithMessage("Code cannot exceed 50 characters.")
            .Matches("^[A-Z0-9_]+$").WithMessage(
                "Code must be uppercase letters, numbers and underscores only.");

        RuleFor(x => x.Description)
            .MaximumLength(300).When(x => x.Description != null);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0);
    }
}

public class CreateDeviceTypeCommandHandler
    : IRequestHandler<CreateDeviceTypeCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateDeviceTypeCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(
        CreateDeviceTypeCommand request,
        CancellationToken cancellationToken)
    {
        // Only CustomerAdmin and above can create custom types
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        // Check for duplicate code within tenant
        var exists = await _context.DeviceTypes
            .AnyAsync(x =>
                x.TenantId == _currentUser.TenantId &&
                x.Code == request.Code.ToUpperInvariant() &&
                !x.IsDeleted,
                cancellationToken);

        if (exists)
            throw new ConflictException(
                $"Device type with code '{request.Code}' already exists.");

        var deviceType = DeviceType.CreateCustomValue(
            tenantId: _currentUser.TenantId,
            name: request.Name,
            code: request.Code,
            description: request.Description,
            icon: request.Icon,
            sortOrder: request.SortOrder);

        _context.Add(deviceType);
        await _context.SaveChangesAsync(cancellationToken);

        return deviceType.Id;
    }
}
