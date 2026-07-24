using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Audit;

public record GetAuditLogsQuery(
    string? EntityType = null,
    string? Action = null,
    DateTime? From = null,
    DateTime? To = null,
    int Take = 200
) : IRequest<List<AuditLogDto>>;

public record AuditLogDto(
    Guid Id,
    string UserName,
    string Action,
    string EntityType,
    Guid EntityId,
    string? EntityDisplay,
    string? ChangesJson,
    DateTime Timestamp
);

/// <summary>
/// Audit trail is admin-only (SuperAdmin + CustomerAdmin, tenant-scoped).
/// </summary>
public class GetAuditLogsQueryHandler
    : IRequestHandler<GetAuditLogsQuery, List<AuditLogDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetAuditLogsQueryHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<AuditLogDto>> Handle(
        GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsSuperAdmin && !_currentUser.IsCustomerAdmin)
            throw new ForbiddenAccessException();

        var query = _context.AuditLogs
            .Where(a => a.TenantId == _currentUser.TenantId);

        if (!string.IsNullOrEmpty(request.EntityType))
            query = query.Where(a => a.EntityType == request.EntityType);
        if (!string.IsNullOrEmpty(request.Action))
            query = query.Where(a => a.Action == request.Action);
        if (request.From is not null)
            query = query.Where(a => a.Timestamp >= request.From);
        if (request.To is not null)
            query = query.Where(a => a.Timestamp <= request.To);

        return await query
            .OrderByDescending(a => a.Timestamp)
            .Take(Math.Clamp(request.Take, 1, 1000))
            .Select(a => new AuditLogDto(
                a.Id, a.UserName, a.Action, a.EntityType, a.EntityId,
                a.EntityDisplay, a.ChangesJson, a.Timestamp))
            .ToListAsync(cancellationToken);
    }
}
