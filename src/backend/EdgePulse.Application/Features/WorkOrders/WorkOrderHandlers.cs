using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.WorkOrders;

// Roles: Executive is read-only. Everyone else in the tenant can create and
// work on orders (operators complete work on the floor). MillManager scoping
// follows their mill.

public record WorkOrderDto(
    Guid Id,
    string Number,
    string Title,
    string? Description,
    Guid DeviceId,
    string DeviceName,
    string DeviceCode,
    Guid MillId,
    Guid? AlertId,
    string Priority,
    string Status,
    string? AssignedTo,
    DateTime? DueDate,
    string? PartsUsed,
    string CreatedBy,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    string? CompletedBy,
    string? CompletionNotes
);

internal static class WorkOrderGuard
{
    public static void RequireWriter(ICurrentUserService user)
    {
        if (user.IsExecutive)
            throw new ForbiddenAccessException();
    }
}

// ── List ─────────────────────────────────────────────────────────────────────

public record GetWorkOrdersQuery(
    string? Status = null,
    Guid? DeviceId = null,
    string? AssignedTo = null,
    int Take = 200
) : IRequest<List<WorkOrderDto>>;

public class GetWorkOrdersQueryHandler
    : IRequestHandler<GetWorkOrdersQuery, List<WorkOrderDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetWorkOrdersQueryHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<WorkOrderDto>> Handle(
        GetWorkOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.WorkOrders
            .Where(w => w.TenantId == _currentUser.TenantId && !w.IsDeleted);

        if (_currentUser.IsMillManager && _currentUser.MillId is not null)
            query = query.Where(w => w.MillId == _currentUser.MillId);

        if (!string.IsNullOrEmpty(request.Status))
            query = query.Where(w => w.Status == request.Status);
        if (request.DeviceId is not null)
            query = query.Where(w => w.DeviceId == request.DeviceId);
        if (!string.IsNullOrEmpty(request.AssignedTo))
            query = query.Where(w => w.AssignedTo == request.AssignedTo);

        return await query
            .OrderByDescending(w => w.CreatedAt)
            .Take(Math.Clamp(request.Take, 1, 500))
            .Join(_context.Devices,
                w => w.DeviceId, d => d.Id,
                (w, d) => new WorkOrderDto(
                    w.Id, w.Number, w.Title, w.Description,
                    w.DeviceId, d.Name, d.Code, w.MillId, w.AlertId,
                    w.Priority, w.Status, w.AssignedTo, w.DueDate, w.PartsUsed,
                    w.CreatedBy, w.CreatedAt,
                    w.CompletedAt, w.CompletedBy, w.CompletionNotes))
            .ToListAsync(cancellationToken);
    }
}

// ── Create ───────────────────────────────────────────────────────────────────

public record CreateWorkOrderCommand(
    Guid DeviceId,
    string Title,
    string? Description,
    string Priority = "MEDIUM",
    Guid? MaintenanceTypeId = null,
    DateTime? DueDate = null,
    string? AssignedTo = null
) : IRequest<Guid>;

public class CreateWorkOrderCommandValidator : AbstractValidator<CreateWorkOrderCommand>
{
    public static readonly string[] Priorities = ["LOW", "MEDIUM", "HIGH", "CRITICAL"];

    public CreateWorkOrderCommandValidator()
    {
        RuleFor(x => x.DeviceId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Priority).Must(p => Priorities.Contains(p))
            .WithMessage($"Priority must be one of: {string.Join(", ", Priorities)}");
        RuleFor(x => x.AssignedTo).MaximumLength(200);
    }
}

public class CreateWorkOrderCommandHandler : IRequestHandler<CreateWorkOrderCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateWorkOrderCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(
        CreateWorkOrderCommand request, CancellationToken cancellationToken)
    {
        WorkOrderGuard.RequireWriter(_currentUser);

        var device = await _context.Devices
            .Where(d => d.Id == request.DeviceId &&
                        d.TenantId == _currentUser.TenantId && !d.IsDeleted)
            .Select(d => new { d.Id, d.MillId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Device), request.DeviceId);

        var workOrder = WorkOrder.Create(
            _currentUser.TenantId, device.Id, device.MillId,
            request.Title,
            createdBy: FirstNonEmpty(_currentUser.FullName, _currentUser.Email, _currentUser.UserId),
            priority: request.Priority,
            description: request.Description,
            maintenanceTypeId: request.MaintenanceTypeId,
            dueDate: request.DueDate);

        if (!string.IsNullOrWhiteSpace(request.AssignedTo))
            workOrder.Assign(request.AssignedTo);

        _context.Add(workOrder);
        await _context.SaveChangesAsync(cancellationToken);
        return workOrder.Id;
    }

    private static string FirstNonEmpty(params string[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "unknown";
}

// ── Transition (start / hold / complete / cancel) ────────────────────────────

public record TransitionWorkOrderCommand(
    Guid Id,
    string Action,          // start | hold | complete | cancel
    string? Notes = null,
    string? PartsUsed = null
) : IRequest;

public class TransitionWorkOrderCommandHandler : IRequestHandler<TransitionWorkOrderCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public TransitionWorkOrderCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        TransitionWorkOrderCommand request, CancellationToken cancellationToken)
    {
        WorkOrderGuard.RequireWriter(_currentUser);

        var workOrder = await _context.WorkOrders
            .FirstOrDefaultAsync(w =>
                w.Id == request.Id &&
                w.TenantId == _currentUser.TenantId && !w.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException(nameof(WorkOrder), request.Id);

        var actor = new[] { _currentUser.FullName, _currentUser.Email, _currentUser.UserId }
            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "unknown";

        try
        {
            switch (request.Action.ToLowerInvariant())
            {
                case "start": workOrder.Start(); break;
                case "hold": workOrder.Hold(); break;
                case "complete":
                    workOrder.Complete(actor, request.Notes, request.PartsUsed);
                    break;
                case "cancel": workOrder.Cancel(); break;
                default:
                    throw new ConflictException(
                        $"Unknown action '{request.Action}'. Valid: start, hold, complete, cancel.");
            }
        }
        catch (InvalidOperationException ex)
        {
            // Illegal lifecycle transition → 409
            throw new ConflictException(ex.Message);
        }

        _context.Update(workOrder);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

// ── Assign ───────────────────────────────────────────────────────────────────

public record AssignWorkOrderCommand(Guid Id, string? AssignedTo) : IRequest;

public class AssignWorkOrderCommandHandler : IRequestHandler<AssignWorkOrderCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public AssignWorkOrderCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(
        AssignWorkOrderCommand request, CancellationToken cancellationToken)
    {
        WorkOrderGuard.RequireWriter(_currentUser);

        var workOrder = await _context.WorkOrders
            .FirstOrDefaultAsync(w =>
                w.Id == request.Id &&
                w.TenantId == _currentUser.TenantId && !w.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException(nameof(WorkOrder), request.Id);

        try
        {
            workOrder.Assign(request.AssignedTo);
        }
        catch (InvalidOperationException ex)
        {
            throw new ConflictException(ex.Message);
        }

        _context.Update(workOrder);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
