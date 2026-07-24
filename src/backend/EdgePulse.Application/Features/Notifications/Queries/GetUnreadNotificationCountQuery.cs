using EdgePulse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Notifications.Queries;

public record GetUnreadNotificationCountQuery : IRequest<int>;

public class GetUnreadNotificationCountQueryHandler
    : IRequestHandler<GetUnreadNotificationCountQuery, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetUnreadNotificationCountQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(
        GetUnreadNotificationCountQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Notifications
            .CountAsync(n =>
                n.TenantId == _currentUser.TenantId &&
                !n.IsRead &&
                !n.IsDeleted,
                cancellationToken);
    }
}
