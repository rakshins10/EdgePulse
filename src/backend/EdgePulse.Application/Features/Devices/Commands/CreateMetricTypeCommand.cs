using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Devices.Commands;

public record CreateMetricTypeCommand(
    string Name,
    string Code,
    string DefaultUnit,
    string? Description,
    int SortOrder = 0
) : IRequest<Guid>;

public class CreateMetricTypeCommandValidator
    : AbstractValidator<CreateMetricTypeCommand>
{
    public CreateMetricTypeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50)
            .Matches("^[A-Z0-9_]+$").WithMessage(
                "Code must be uppercase letters, numbers and underscores only.");
        RuleFor(x => x.DefaultUnit).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Description).MaximumLength(300)
            .When(x => x.Description != null);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public class CreateMetricTypeCommandHandler
    : IRequestHandler<CreateMetricTypeCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateMetricTypeCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(
        CreateMetricTypeCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        var exists = await _context.MetricTypes
            .AnyAsync(x =>
                x.TenantId == _currentUser.TenantId &&
                x.Code == request.Code.ToUpperInvariant() &&
                !x.IsDeleted, cancellationToken);

        if (exists)
            throw new ConflictException(
                $"Metric type with code '{request.Code}' already exists.");

        var metricType = MetricType.CreateCustomValue(
            tenantId: _currentUser.TenantId,
            name: request.Name,
            code: request.Code,
            defaultUnit: request.DefaultUnit,
            description: request.Description,
            sortOrder: request.SortOrder);

        _context.Add(metricType);
        await _context.SaveChangesAsync(cancellationToken);
        return metricType.Id;
    }
}
