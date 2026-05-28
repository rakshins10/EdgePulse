using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Alerts.Commands;

public record CreateAlertThresholdCommand(
    Guid DeviceId,
    string MetricKey,
    string Name,
    double? MinValue,
    double? MaxValue,
    string SeverityCode,
    string? Unit,
    int ConsecutiveCount,
    string? Description
) : IRequest<Guid>;

public class CreateAlertThresholdCommandValidator
    : AbstractValidator<CreateAlertThresholdCommand>
{
    public CreateAlertThresholdCommandValidator()
    {
        RuleFor(x => x.DeviceId)
            .NotEmpty().WithMessage("Device is required.");

        RuleFor(x => x.MetricKey)
            .NotEmpty().WithMessage("Metric key is required.")
            .MaximumLength(100);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200);

        RuleFor(x => x)
            .Must(x => x.MinValue.HasValue || x.MaxValue.HasValue)
            .WithMessage("At least one of MinValue or MaxValue must be specified.");

        RuleFor(x => x.SeverityCode)
            .NotEmpty().WithMessage("Severity is required.")
            .Must(c => new[] { "CRITICAL", "HIGH", "MEDIUM", "LOW" }.Contains(c))
            .WithMessage("SeverityCode must be CRITICAL, HIGH, MEDIUM or LOW.");

        RuleFor(x => x.ConsecutiveCount)
            .GreaterThanOrEqualTo(1)
            .WithMessage("ConsecutiveCount must be at least 1.");

        RuleFor(x => x.Unit)
            .MaximumLength(20).When(x => x.Unit != null);

        RuleFor(x => x.Description)
            .MaximumLength(500).When(x => x.Description != null);
    }
}

public class CreateAlertThresholdCommandHandler
    : IRequestHandler<CreateAlertThresholdCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateAlertThresholdCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(
        CreateAlertThresholdCommand request,
        CancellationToken cancellationToken)
    {
        // Operators and Executives cannot manage thresholds
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        // Verify device belongs to tenant
        var device = await _context.Devices
            .FirstOrDefaultAsync(x =>
                x.Id == request.DeviceId &&
                x.TenantId == _currentUser.TenantId &&
                !x.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException(nameof(Device), request.DeviceId);

        // MillManager can only configure thresholds in their mill
        if (_currentUser.IsMillManager &&
            _currentUser.MillId != device.MillId)
            throw new ForbiddenAccessException();

        var threshold = AlertThreshold.Create(
            tenantId: _currentUser.TenantId,
            deviceId: request.DeviceId,
            metricKey: request.MetricKey,
            name: request.Name,
            minValue: request.MinValue,
            maxValue: request.MaxValue,
            severityCode: request.SeverityCode,
            unit: request.Unit,
            consecutiveCount: request.ConsecutiveCount,
            description: request.Description);

        _context.Add(threshold);
        await _context.SaveChangesAsync(cancellationToken);

        return threshold.Id;
    }
}
