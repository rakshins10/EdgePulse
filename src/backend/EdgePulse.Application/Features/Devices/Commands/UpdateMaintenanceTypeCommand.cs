using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Devices.Commands;

public record UpdateMaintenanceTypeCommand(
    Guid Id,
    string Name,
    string? Description,
    string? Color,
    int SortOrder = 0
) : IRequest;

public class UpdateMaintenanceTypeCommandValidator
    : AbstractValidator<UpdateMaintenanceTypeCommand>
{
    public UpdateMaintenanceTypeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(300).When(x => x.Description != null);
        RuleFor(x => x.Color)
            .Matches("^#[0-9A-Fa-f]{6}$").When(x => x.Color != null)
            .WithMessage("Color must be a valid hex code e.g. #22c55e");
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public class UpdateMaintenanceTypeCommandHandler
    : IRequestHandler<UpdateMaintenanceTypeCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateMaintenanceTypeCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        UpdateMaintenanceTypeCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        var maintenanceType = await _context.MaintenanceTypes
            .FirstOrDefaultAsync(x =>
                x.Id == request.Id &&
                x.TenantId == _currentUser.TenantId &&
                !x.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException(nameof(MaintenanceType), request.Id);

        // System values cannot be edited directly (tenants use a TenantLookupOverride).
        if (maintenanceType.IsSystem)
            throw new ForbiddenAccessException();

        maintenanceType.UpdateDetails(request.Name, request.Description);
        maintenanceType.UpdateColor(request.Color);
        _context.Update(maintenanceType);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
