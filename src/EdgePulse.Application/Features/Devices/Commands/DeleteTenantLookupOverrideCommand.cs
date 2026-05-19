using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Devices.Commands;

/// <summary>
/// Removes a tenant override -- restores the template default.
/// </summary>
public record DeleteTenantLookupOverrideCommand(Guid Id) : IRequest;

public class DeleteTenantLookupOverrideCommandHandler
    : IRequestHandler<DeleteTenantLookupOverrideCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DeleteTenantLookupOverrideCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        DeleteTenantLookupOverrideCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        var override_ = await _context.TenantLookupOverrides
            .FirstOrDefaultAsync(x =>
                x.Id == request.Id &&
                x.TenantId == _currentUser.TenantId,
                cancellationToken)
            ?? throw new NotFoundException(
                nameof(TenantLookupOverride), request.Id);

        _context.Remove(override_);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
