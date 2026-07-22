using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Devices.Commands;

public record UpdateMetricTypeCommand(
    Guid Id,
    string Name,
    string DefaultUnit,
    string? Description,
    int SortOrder = 0
) : IRequest;

public class UpdateMetricTypeCommandValidator
    : AbstractValidator<UpdateMetricTypeCommand>
{
    public UpdateMetricTypeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(100);
        RuleFor(x => x.DefaultUnit).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Description).MaximumLength(300).When(x => x.Description != null);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public class UpdateMetricTypeCommandHandler
    : IRequestHandler<UpdateMetricTypeCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateMetricTypeCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        UpdateMetricTypeCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        var metricType = await _context.MetricTypes
            .FirstOrDefaultAsync(x =>
                x.Id == request.Id &&
                x.TenantId == _currentUser.TenantId &&
                !x.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException(nameof(MetricType), request.Id);

        // System values cannot be edited directly (tenants use a TenantLookupOverride).
        if (metricType.IsSystem)
            throw new ForbiddenAccessException();

        metricType.UpdateDetails(request.Name, request.Description);
        metricType.UpdateDefaultUnit(request.DefaultUnit);
        _context.Update(metricType);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
