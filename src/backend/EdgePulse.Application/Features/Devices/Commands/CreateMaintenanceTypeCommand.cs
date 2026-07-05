using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Devices.Commands;

public record CreateMaintenanceTypeCommand(
    string Name,
    string Code,
    string? Description,
    string? Color,
    int SortOrder = 0
) : IRequest<Guid>;

public class CreateMaintenanceTypeCommandValidator
    : AbstractValidator<CreateMaintenanceTypeCommand>
{
    public CreateMaintenanceTypeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50)
            .Matches("^[A-Z0-9_]+$").WithMessage(
                "Code must be uppercase letters, numbers and underscores only.");
        RuleFor(x => x.Description).MaximumLength(300)
            .When(x => x.Description != null);
        RuleFor(x => x.Color)
            .Matches("^#[0-9A-Fa-f]{6}$").When(x => x.Color != null)
            .WithMessage("Color must be a valid hex code e.g. #22c55e");
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public class CreateMaintenanceTypeCommandHandler
    : IRequestHandler<CreateMaintenanceTypeCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateMaintenanceTypeCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(
        CreateMaintenanceTypeCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        var exists = await _context.MaintenanceTypes
            .AnyAsync(x =>
                x.TenantId == _currentUser.TenantId &&
                x.Code == request.Code.ToUpperInvariant() &&
                !x.IsDeleted, cancellationToken);

        if (exists)
            throw new ConflictException(
                $"Maintenance type with code '{request.Code}' already exists.");

        var maintenanceType = MaintenanceType.CreateCustomValue(
            tenantId: _currentUser.TenantId,
            name: request.Name,
            code: request.Code,
            color: request.Color,
            description: request.Description,
            sortOrder: request.SortOrder);

        _context.Add(maintenanceType);
        await _context.SaveChangesAsync(cancellationToken);
        return maintenanceType.Id;
    }
}
