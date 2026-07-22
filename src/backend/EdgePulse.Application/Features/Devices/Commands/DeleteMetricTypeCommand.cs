using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Devices.Commands;

public record DeleteMetricTypeCommand(Guid Id) : IRequest;

public class DeleteMetricTypeCommandHandler
    : IRequestHandler<DeleteMetricTypeCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DeleteMetricTypeCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        DeleteMetricTypeCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        var metricType = await _context.MetricTypes
            .FirstOrDefaultAsync(x =>
                x.Id == request.Id &&
                !x.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException(nameof(MetricType), request.Id);

        // System values cannot be deleted.
        if (metricType.IsSystem)
            throw new ForbiddenAccessException();

        // Must belong to the current tenant.
        if (metricType.TenantId != _currentUser.TenantId)
            throw new ForbiddenAccessException();

        metricType.Deactivate();
        _context.Update(metricType);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
