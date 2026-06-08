using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Areas.Commands;

public record DeleteAreaCommand(Guid Id) : IRequest;

public class DeleteAreaCommandHandler : IRequestHandler<DeleteAreaCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DeleteAreaCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteAreaCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        var area = await _context.Areas
            .FirstOrDefaultAsync(x =>
                x.Id == request.Id &&
                x.TenantId == _currentUser.TenantId &&
                !x.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException(nameof(Area), request.Id);

        if (_currentUser.IsMillManager &&
            _currentUser.MillId.HasValue &&
            _currentUser.MillId.Value != area.MillId)
            throw new ForbiddenAccessException();

        // Guard: refuse delete if area has active devices
        var activeDevices = await _context.Devices
            .CountAsync(d =>
                d.AreaId == area.Id &&
                !d.IsDeleted,
                cancellationToken);

        if (activeDevices > 0)
            throw new ConflictException(
                $"Area has {activeDevices} active device(s). Decommission or move them first.");

        area.MarkAsDeleted();
        _context.Update(area);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
