using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Notifications.Commands;

public record MarkNotificationReadCommand(Guid Id) : IRequest;

public class MarkNotificationReadCommandHandler
    : IRequestHandler<MarkNotificationReadCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public MarkNotificationReadCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        MarkNotificationReadCommand request,
        CancellationToken cancellationToken)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n =>
                n.Id == request.Id &&
                n.TenantId == _currentUser.TenantId &&
                !n.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException(nameof(Notification), request.Id);

        notification.MarkRead();
        _context.Update(notification);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
