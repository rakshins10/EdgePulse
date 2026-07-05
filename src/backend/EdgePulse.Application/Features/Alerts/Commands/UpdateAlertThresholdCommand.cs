using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Alerts.Commands;

public record UpdateAlertThresholdCommand(
    Guid Id,
    string Name,
    double? MinValue,
    double? MaxValue,
    string SeverityCode,
    string? Unit,
    int ConsecutiveCount,
    string? Description
) : IRequest;

public class UpdateAlertThresholdCommandValidator
    : AbstractValidator<UpdateAlertThresholdCommand>
{
    public UpdateAlertThresholdCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200);

        RuleFor(x => x)
            .Must(x => x.MinValue.HasValue || x.MaxValue.HasValue)
            .WithMessage("At least one of MinValue or MaxValue must be specified.");

        RuleFor(x => x.SeverityCode)
            .NotEmpty()
            .Must(c => new[] { "CRITICAL", "HIGH", "MEDIUM", "LOW" }.Contains(c))
            .WithMessage("SeverityCode must be CRITICAL, HIGH, MEDIUM or LOW.");

        RuleFor(x => x.ConsecutiveCount)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.Unit)
            .MaximumLength(20).When(x => x.Unit != null);

        RuleFor(x => x.Description)
            .MaximumLength(500).When(x => x.Description != null);
    }
}

public class UpdateAlertThresholdCommandHandler
    : IRequestHandler<UpdateAlertThresholdCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateAlertThresholdCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        UpdateAlertThresholdCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        var threshold = await _context.AlertThresholds
            .FirstOrDefaultAsync(x =>
                x.Id == request.Id &&
                x.TenantId == _currentUser.TenantId &&
                !x.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException(nameof(AlertThreshold), request.Id);

        // MillManager scope check via device
        if (_currentUser.IsMillManager)
        {
            var device = await _context.Devices
                .FirstOrDefaultAsync(x => x.Id == threshold.DeviceId, cancellationToken);
            if (device?.MillId != _currentUser.MillId)
                throw new ForbiddenAccessException();
        }

        threshold.Update(
            name: request.Name,
            minValue: request.MinValue,
            maxValue: request.MaxValue,
            severityCode: request.SeverityCode,
            unit: request.Unit,
            consecutiveCount: request.ConsecutiveCount,
            description: request.Description);

        _context.Update(threshold);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
