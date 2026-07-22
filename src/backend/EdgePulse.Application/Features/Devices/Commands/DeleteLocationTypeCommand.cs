using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Devices.Commands;

public record DeleteLocationTypeCommand(Guid Id) : IRequest;

public class DeleteLocationTypeCommandHandler
    : IRequestHandler<DeleteLocationTypeCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DeleteLocationTypeCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        DeleteLocationTypeCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        var locationType = await _context.LocationTypes
            .FirstOrDefaultAsync(x =>
                x.Id == request.Id &&
                !x.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException(nameof(LocationType), request.Id);

        // System values cannot be deleted.
        if (locationType.IsSystem)
            throw new ForbiddenAccessException();

        // Must belong to the current tenant.
        if (locationType.TenantId != _currentUser.TenantId)
            throw new ForbiddenAccessException();

        // Cannot delete a location type still assigned to an area.
        var inUse = await _context.Areas
            .AnyAsync(x =>
                x.LocationTypeId == request.Id &&
                !x.IsDeleted,
                cancellationToken);

        if (inUse)
            throw new ConflictException(
                "Cannot delete a location type assigned to active areas.");

        locationType.Deactivate();
        _context.Update(locationType);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
