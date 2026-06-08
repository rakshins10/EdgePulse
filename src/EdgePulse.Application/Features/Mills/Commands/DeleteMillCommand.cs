using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Mills.Commands;

public record DeleteMillCommand(Guid Id) : IRequest;

public class DeleteMillCommandHandler : IRequestHandler<DeleteMillCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DeleteMillCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteMillCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.IsOperator || _currentUser.IsExecutive || _currentUser.IsMillManager)
            throw new ForbiddenAccessException();

        // SuperAdmin can delete mills across all tenants (matches GetMillsQuery);
        // CustomerAdmin is restricted to their own tenant.
        var query = _context.Mills.Where(x => x.Id == request.Id && !x.IsDeleted);
        if (!_currentUser.IsSuperAdmin)
            query = query.Where(x => x.TenantId == _currentUser.TenantId);

        var mill = await query.FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Mill), request.Id);

        // Guard: refuse delete if mill has active areas
        var activeAreas = await _context.Areas
            .CountAsync(a =>
                a.MillId == mill.Id &&
                !a.IsDeleted,
                cancellationToken);

        if (activeAreas > 0)
            throw new ConflictException(
                $"Mill has {activeAreas} active area(s). Delete or move them first.");

        // Guard: refuse delete if mill has active devices
        var activeDevices = await _context.Devices
            .CountAsync(d =>
                d.MillId == mill.Id &&
                !d.IsDeleted,
                cancellationToken);

        if (activeDevices > 0)
            throw new ConflictException(
                $"Mill has {activeDevices} active device(s). Decommission them first.");

        mill.MarkAsDeleted();
        _context.Update(mill);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
