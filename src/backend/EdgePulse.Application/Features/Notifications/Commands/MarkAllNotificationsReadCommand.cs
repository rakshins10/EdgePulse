using EdgePulse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Notifications.Commands;

public record MarkAllNotificationsReadCommand : IRequest<int>;

public class MarkAllNotificationsReadCommandHandler
    : IRequestHandler<MarkAllNotificationsReadCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public MarkAllNotificationsReadCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(
        MarkAllNotificationsReadCommand request,
        CancellationToken cancellationToken)
    {
        var unread = await _context.Notifications
            .Where(n =>
                n.TenantId == _currentUser.TenantId &&
                !n.IsRead &&
                !n.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var notification in unread)
        {
            notification.MarkRead();
            _context.Update(notification);
        }

        if (unread.Count > 0)
            await _context.SaveChangesAsync(cancellationToken);

        return unread.Count;
    }
}
