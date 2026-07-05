using EdgePulse.Application.Features.Alerts.Commands;
using EdgePulse.Application.Features.Alerts.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdgePulse.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AlertsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AlertsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ─── Alert Thresholds ────────────────────────────────────────────────────

    /// <summary>
    /// Get all alert thresholds for the current tenant.
    /// Optionally filter by device. Inactive thresholds excluded by default.
    /// Role-scoped: MillManager sees their mill, Operator sees assigned areas.
    /// </summary>
    [HttpGet("thresholds")]
    [ProducesResponseType(typeof(List<AlertThresholdDto>), 200)]
    public async Task<IActionResult> GetThresholds(
        [FromQuery] Guid? deviceId,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetAlertThresholdsQuery(deviceId, includeInactive),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a new alert threshold for a device metric.
    /// CustomerAdmin and MillManager only.
    /// </summary>
    [HttpPost("thresholds")]
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CreateThreshold(
        [FromBody] CreateAlertThresholdRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            new CreateAlertThresholdCommand(
                request.DeviceId,
                request.MetricKey,
                request.Name,
                request.MinValue,
                request.MaxValue,
                request.SeverityCode,
                request.Unit,
                request.ConsecutiveCount,
                request.Description),
            cancellationToken);
        return CreatedAtAction(nameof(GetThresholds), new { }, new { id });
    }

    /// <summary>
    /// Update an existing alert threshold.
    /// CustomerAdmin and MillManager only.
    /// </summary>
    [HttpPut("thresholds/{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateThreshold(
        Guid id,
        [FromBody] UpdateAlertThresholdRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpdateAlertThresholdCommand(
                id,
                request.Name,
                request.MinValue,
                request.MaxValue,
                request.SeverityCode,
                request.Unit,
                request.ConsecutiveCount,
                request.Description),
            cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Delete (soft-delete + deactivate) an alert threshold.
    /// CustomerAdmin and MillManager only.
    /// Historical alerts referencing this threshold are preserved.
    /// </summary>
    [HttpDelete("thresholds/{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteThreshold(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new DeleteAlertThresholdCommand(id),
            cancellationToken);
        return NoContent();
    }

    // ─── Alerts ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Get alerts with optional filters.
    /// Role-scoped: MillManager sees their mill, Operator sees assigned areas.
    /// Ordered by TriggeredAt descending. Paginated.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(AlertListResult), 200)]
    public async Task<IActionResult> GetAlerts(
        [FromQuery] Guid? millId,
        [FromQuery] Guid? deviceId,
        [FromQuery] string? severityCode,
        [FromQuery] string? statusCode,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetAlertsQuery(millId, deviceId, severityCode, statusCode,
                page, pageSize),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get the count of OPEN alerts — used by the sidebar badge.
    /// Returns OpenCount and CriticalOpenCount.
    /// Polled every 30s by the frontend.
    /// </summary>
    [HttpGet("count")]
    [ProducesResponseType(typeof(AlertCountDto), 200)]
    public async Task<IActionResult> GetAlertCount(
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetAlertCountQuery(),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Acknowledge an open alert.
    /// Transitions: OPEN → ACKNOWLEDGED.
    /// Operators, MillManagers, CustomerAdmins can acknowledge.
    /// </summary>
    [HttpPost("{id:guid}/acknowledge")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AcknowledgeAlert(
        Guid id,
        [FromBody] AlertActionRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new AcknowledgeAlertCommand(id, request.Notes),
            cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Resolve an alert.
    /// Transitions: OPEN → RESOLVED or ACKNOWLEDGED → RESOLVED.
    /// Operators, MillManagers, CustomerAdmins can resolve.
    /// </summary>
    [HttpPost("{id:guid}/resolve")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ResolveAlert(
        Guid id,
        [FromBody] AlertActionRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new ResolveAlertCommand(id, request.Notes),
            cancellationToken);
        return NoContent();
    }
}

// ─── Request Records ─────────────────────────────────────────────────────────

public record CreateAlertThresholdRequest(
    Guid DeviceId,
    string MetricKey,
    string Name,
    double? MinValue,
    double? MaxValue,
    string SeverityCode,
    string? Unit = null,
    int ConsecutiveCount = 3,
    string? Description = null
);

public record UpdateAlertThresholdRequest(
    string Name,
    double? MinValue,
    double? MaxValue,
    string SeverityCode,
    string? Unit = null,
    int ConsecutiveCount = 3,
    string? Description = null
);

public record AlertActionRequest(
    string? Notes = null
);
