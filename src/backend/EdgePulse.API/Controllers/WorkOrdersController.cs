using EdgePulse.Application.Features.WorkOrders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdgePulse.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class WorkOrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public WorkOrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// List work orders (newest first). Filter by status, device or assignee.
    /// MillManager sees their mill only; also serves per-device maintenance history.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<WorkOrderDto>), 200)]
    public async Task<IActionResult> GetWorkOrders(
        [FromQuery] string? status,
        [FromQuery] Guid? deviceId,
        [FromQuery] string? assignedTo,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetWorkOrdersQuery(status, deviceId, assignedTo), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a work order manually.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Create(
        [FromBody] CreateWorkOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var id = await _mediator.Send(
            new CreateWorkOrderCommand(
                request.DeviceId, request.Title, request.Description,
                request.Priority ?? "MEDIUM", request.MaintenanceTypeId,
                request.DueDate, request.AssignedTo),
            cancellationToken);
        return CreatedAtAction(nameof(GetWorkOrders), new { }, id);
    }

    /// <summary>
    /// Lifecycle transition: start, hold, complete (notes/parts) or cancel.
    /// Illegal transitions return 409.
    /// </summary>
    [HttpPost("{id:guid}/transition")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Transition(
        Guid id,
        [FromBody] TransitionWorkOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        await _mediator.Send(
            new TransitionWorkOrderCommand(
                id, request.Action, request.Notes, request.PartsUsed),
            cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Assign (or unassign with null/empty) a technician.
    /// </summary>
    [HttpPut("{id:guid}/assign")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Assign(
        Guid id,
        [FromBody] AssignWorkOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        await _mediator.Send(
            new AssignWorkOrderCommand(id, request.AssignedTo), cancellationToken);
        return NoContent();
    }
}

public record CreateWorkOrderRequest(
    Guid DeviceId,
    string Title,
    string? Description,
    string? Priority,
    Guid? MaintenanceTypeId,
    DateTime? DueDate,
    string? AssignedTo
);

public record TransitionWorkOrderRequest(
    string Action,
    string? Notes,
    string? PartsUsed
);

public record AssignWorkOrderRequest(string? AssignedTo);
