using EdgePulse.Application.Features.Alerts.Queries;
using EdgePulse.Application.Features.Devices.Commands;
using EdgePulse.Application.Features.Devices.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EdgePulse.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigurationController : ControllerBase
{
    private readonly IMediator _mediator;

    public ConfigurationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // =============================================
    // DEVICE TYPES
    // =============================================

    /// <summary>
    /// Get all device types for current tenant.
    /// Returns system defaults + tenant custom types.
    /// </summary>
    [HttpGet("device-types")]
    [ProducesResponseType(typeof(List<DeviceTypeDto>), 200)]
    public async Task<IActionResult> GetDeviceTypes(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetDeviceTypesQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a custom device type for current tenant.
    /// CustomerAdmin and above only.
    /// System device types cannot be created via this endpoint.
    /// </summary>
    [HttpPost("device-types")]
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> CreateDeviceType(
        [FromBody] CreateDeviceTypeCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(
            nameof(GetDeviceTypes), new { }, id);
    }

    /// <summary>
    /// Update a custom device type.
    /// Only tenant-owned types can be updated.
    /// System types use TenantLookupOverride for renaming.
    /// </summary>
    [HttpPut("device-types/{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateDeviceType(
        Guid id,
        [FromBody] UpdateDeviceTypeRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpdateDeviceTypeCommand(
                id,
                request.Name,
                request.Description,
                request.Icon,
                request.SortOrder),
            cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Deactivate a custom device type.
    /// Only tenant-owned types can be deleted.
    /// System types use TenantLookupOverride to disable.
    /// Cannot delete if devices are actively using this type.
    /// </summary>
    [HttpDelete("device-types/{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> DeleteDeviceType(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new DeleteDeviceTypeCommand(id), cancellationToken);
        return NoContent();
    }

    // =============================================
    // DEVICE STATUSES
    // =============================================

    /// <summary>
    /// Get all device statuses for current tenant.
    /// </summary>
    [HttpGet("device-statuses")]
    [ProducesResponseType(typeof(List<DeviceStatusDto>), 200)]
    public async Task<IActionResult> GetDeviceStatuses(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetDeviceStatusesQuery(), cancellationToken);
        return Ok(result);
    }

    // =============================================
    // METRIC TYPES
    // =============================================

    /// <summary>
    /// Get all metric types for current tenant.
    /// </summary>
    [HttpGet("metric-types")]
    [ProducesResponseType(typeof(List<MetricTypeDto>), 200)]
    public async Task<IActionResult> GetMetricTypes(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetMetricTypesQuery(), cancellationToken);
        return Ok(result);
    }

    // =============================================
    // ALERT SEVERITIES
    // =============================================

    /// <summary>
    /// Get all alert severities for current tenant.
    /// Ordered by priority (Critical first).
    /// </summary>
    [HttpGet("alert-severities")]
    [ProducesResponseType(typeof(List<AlertSeverityDto>), 200)]
    public async Task<IActionResult> GetAlertSeverities(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetAlertSeveritiesQuery(), cancellationToken);
        return Ok(result);
    }

    // =============================================
    // ALERT STATUSES
    // =============================================

    /// <summary>
    /// Get all alert statuses for current tenant.
    /// </summary>
    [HttpGet("alert-statuses")]
    [ProducesResponseType(typeof(List<AlertStatusDto>), 200)]
    public async Task<IActionResult> GetAlertStatuses(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetAlertStatusesQuery(), cancellationToken);
        return Ok(result);
    }

    // =============================================
    // INDUSTRY TEMPLATES (SuperAdmin only)
    // =============================================

    /// <summary>
    /// Get all industry templates.
    /// SuperAdmin only.
    /// </summary>
    [HttpGet("industry-templates")]
    [ProducesResponseType(typeof(List<IndustryTemplateDto>), 200)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetIndustryTemplates(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetIndustryTemplatesQuery(), cancellationToken);
        return Ok(result);
    }
}

// Request model for PUT (separate from command to allow route id)
public record UpdateDeviceTypeRequest(
    string Name,
    string? Description,
    string? Icon,
    int SortOrder
);
