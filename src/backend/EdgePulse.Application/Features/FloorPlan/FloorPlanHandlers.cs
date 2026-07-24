using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.FloorPlan;

public record FloorPlanDeviceDto(
    Guid DeviceId,
    string Name,
    string Code,
    string AreaName,
    string StatusName,
    string? StatusColor,
    double? FloorX,
    double? FloorY,
    int OpenAlerts,
    int CriticalAlerts
);

// ── Query ────────────────────────────────────────────────────────────────────

public record GetFloorPlanQuery(Guid MillId) : IRequest<List<FloorPlanDeviceDto>>;

public class GetFloorPlanQueryHandler
    : IRequestHandler<GetFloorPlanQuery, List<FloorPlanDeviceDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetFloorPlanQueryHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<FloorPlanDeviceDto>> Handle(
        GetFloorPlanQuery request, CancellationToken cancellationToken)
    {
        var devices = await _context.Devices
            .Where(d => d.TenantId == _currentUser.TenantId &&
                        d.MillId == request.MillId && !d.IsDeleted)
            .Join(_context.Areas, d => d.AreaId, a => a.Id,
                (d, a) => new { d.Id, d.Name, d.Code, AreaName = a.Name, d.StatusId, d.FloorX, d.FloorY })
            .Join(_context.DeviceStatuses, d => d.StatusId, s => s.Id,
                (d, s) => new
                {
                    d.Id, d.Name, d.Code, d.AreaName, d.FloorX, d.FloorY,
                    StatusName = s.Name, StatusColor = s.Color,
                })
            .ToListAsync(cancellationToken);

        var alertCounts = await _context.Alerts
            .Where(a => a.TenantId == _currentUser.TenantId &&
                        a.MillId == request.MillId && !a.IsDeleted &&
                        (a.StatusCode == "OPEN" || a.StatusCode == "ACKNOWLEDGED"))
            .GroupBy(a => a.DeviceId)
            .Select(g => new
            {
                DeviceId = g.Key,
                Open = g.Count(),
                Critical = g.Count(a => a.SeverityCode == "CRITICAL"),
            })
            .ToListAsync(cancellationToken);

        return devices
            .Select(d =>
            {
                var alerts = alertCounts.FirstOrDefault(a => a.DeviceId == d.Id);
                return new FloorPlanDeviceDto(
                    d.Id, d.Name, d.Code, d.AreaName,
                    d.StatusName, d.StatusColor,
                    d.FloorX, d.FloorY,
                    alerts?.Open ?? 0, alerts?.Critical ?? 0);
            })
            .OrderBy(d => d.Name)
            .ToList();
    }
}

// ── Position command ─────────────────────────────────────────────────────────

public record SetDevicePositionCommand(Guid DeviceId, double? X, double? Y) : IRequest;

public class SetDevicePositionCommandHandler : IRequestHandler<SetDevicePositionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SetDevicePositionCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        SetDevicePositionCommand request, CancellationToken cancellationToken)
    {
        // Layout editing: admins + mill managers (their operational map)
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        var device = await _context.Devices
            .FirstOrDefaultAsync(d =>
                d.Id == request.DeviceId &&
                d.TenantId == _currentUser.TenantId && !d.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException(nameof(Device), request.DeviceId);

        device.SetFloorPosition(request.X, request.Y);
        _context.Update(device);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
