using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Constants;
using EdgePulse.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Devices.Commands;

/// <summary>
/// Creates or updates a tenant override for a template lookup value.
/// Used to rename or disable template values for a specific tenant.
/// </summary>
public record UpsertTenantLookupOverrideCommand(
    string LookupType,
    Guid LookupId,
    string? DisplayName,
    bool IsActive
) : IRequest<Guid>;

public class UpsertTenantLookupOverrideCommandValidator
    : AbstractValidator<UpsertTenantLookupOverrideCommand>
{
    private static readonly string[] ValidLookupTypes =
    {
        LookupTypes.DeviceType,
        LookupTypes.DeviceStatus,
        LookupTypes.AlertSeverity,
        LookupTypes.AlertStatus,
        LookupTypes.MetricType,
        LookupTypes.Unit,
        LookupTypes.MaintenanceType,
        LookupTypes.LocationType,
        LookupTypes.DeviceManufacturer,
        LookupTypes.DeviceModel
    };

    public UpsertTenantLookupOverrideCommandValidator()
    {
        RuleFor(x => x.LookupType)
            .NotEmpty()
            .Must(t => ValidLookupTypes.Contains(t))
            .WithMessage(
                $"LookupType must be one of: " +
                string.Join(", ", ValidLookupTypes));

        RuleFor(x => x.LookupId)
            .NotEmpty().WithMessage("LookupId is required.");

        RuleFor(x => x.DisplayName)
            .MaximumLength(100).When(x => x.DisplayName != null)
            .WithMessage("DisplayName cannot exceed 100 characters.");
    }
}

public class UpsertTenantLookupOverrideCommandHandler
    : IRequestHandler<UpsertTenantLookupOverrideCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpsertTenantLookupOverrideCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(
        UpsertTenantLookupOverrideCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        // Check if override already exists for this tenant + lookup
        var existing = await _context.TenantLookupOverrides
            .FirstOrDefaultAsync(x =>
                x.TenantId == _currentUser.TenantId &&
                x.LookupType == request.LookupType &&
                x.LookupId == request.LookupId,
                cancellationToken);

        if (existing != null)
        {
            // Update existing override
            if (request.DisplayName != null)
                existing.Rename(request.DisplayName,
                    _currentUser.UserId);

            if (!request.IsActive)
                existing.Deactivate(_currentUser.UserId);
            else
                existing.Reactivate(_currentUser.UserId);

            _context.Update(existing);
            await _context.SaveChangesAsync(cancellationToken);
            return existing.Id;
        }

        // Create new override
        var override_ = TenantLookupOverride.Create(
            tenantId: _currentUser.TenantId,
            lookupType: request.LookupType,
            lookupId: request.LookupId,
            updatedBy: _currentUser.UserId,
            displayName: request.DisplayName,
            isActive: request.IsActive);

        _context.Add(override_);
        await _context.SaveChangesAsync(cancellationToken);
        return override_.Id;
    }
}
