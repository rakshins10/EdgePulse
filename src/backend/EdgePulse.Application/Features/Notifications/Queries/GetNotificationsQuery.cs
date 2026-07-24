using EdgePulse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Notifications.Queries;

public record GetNotificationsQuery(
    bool UnreadOnly = false,
    int Take = 50
) : IRequest<List<NotificationDto>>;

public record NotificationDto(
    Guid Id,
    string Type,
    string? SeverityCode,
    string Title,
    string Message,
    string? LinkEntityType,
    Guid? LinkEntityId,
    bool IsRead,
    DateTime CreatedAt
);

public class GetNotificationsQueryHandler
    : IRequestHandler<GetNotificationsQuery, List<NotificationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetNotificationsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<NotificationDto>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.Take, 1, 200);

        var query = _context.Notifications
            .Where(n =>
                n.TenantId == _currentUser.TenantId &&
                !n.IsDeleted);

        if (request.UnreadOnly)
            query = query.Where(n => !n.IsRead);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .Select(n => new NotificationDto(
                n.Id, n.Type, n.SeverityCode,
                n.Title, n.Message,
                n.LinkEntityType, n.LinkEntityId,
                n.IsRead, n.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
