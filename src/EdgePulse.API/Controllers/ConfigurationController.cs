using EdgePulse.Application.Features.Alerts.Queries;
using EdgePulse.Application.Features.Devices.Commands;
using EdgePulse.Application.Features.Alerts.Commands;
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

    [HttpGet("device-types")]
    [ProducesResponseType(typeof(List<DeviceTypeDto>), 200)]
    public async Task<IActionResult> GetDeviceTypes(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetDeviceTypesQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("device-types")]
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> CreateDeviceType([FromBody] CreateDeviceTypeRequest request, CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            new CreateDeviceTypeCommand(
                request.Name, request.Code,
                request.Description, request.Icon,
                request.SortOrder),
            cancellationToken);
        return CreatedAtAction(nameof(GetDeviceTypes), new { }, id);
    }

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
                id, request.Name, request.Description,
                request.Icon, request.SortOrder),
            cancellationToken);
        return NoContent();
    }

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

    [HttpGet("device-statuses")]
    [ProducesResponseType(typeof(List<DeviceStatusDto>), 200)]
    public async Task<IActionResult> GetDeviceStatuses(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetDeviceStatusesQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("device-statuses")]
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> CreateDeviceStatus(
    [FromBody] CreateDeviceStatusRequest request,
    CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            new CreateDeviceStatusCommand(
                request.Name, request.Code,
                request.Description, request.Color,
                request.SortOrder),
            cancellationToken);
        return CreatedAtAction(nameof(GetDeviceStatuses), new { }, id);
    }

    [HttpPut("device-statuses/{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateDeviceStatus(
        Guid id,
        [FromBody] UpdateDeviceStatusRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpdateDeviceStatusCommand(
                id, request.Name, request.Description,
                request.Color, request.SortOrder),
            cancellationToken);
        return NoContent();
    }

    [HttpDelete("device-statuses/{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> DeleteDeviceStatus(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new DeleteDeviceStatusCommand(id), cancellationToken);
        return NoContent();
    }

    // =============================================
    // METRIC TYPES
    // =============================================

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

    [HttpGet("alert-severities")]
    [ProducesResponseType(typeof(List<AlertSeverityDto>), 200)]
    public async Task<IActionResult> GetAlertSeverities(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetAlertSeveritiesQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("alert-severities")]
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> CreateAlertSeverity(
        [FromBody] CreateAlertSeverityRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            new CreateAlertSeverityCommand(
                request.Name, request.Code, request.Description,
                request.Color, request.Priority, request.SortOrder),
            cancellationToken);
        return CreatedAtAction(nameof(GetAlertSeverities), new { }, id);
    }

    [HttpPut("alert-severities/{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateAlertSeverity(
        Guid id,
        [FromBody] UpdateAlertSeverityRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpdateAlertSeverityCommand(
                id, request.Name, request.Description,
                request.Color, request.Priority, request.SortOrder),
            cancellationToken);
        return NoContent();
    }

    [HttpDelete("alert-severities/{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteAlertSeverity(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new DeleteAlertSeverityCommand(id), cancellationToken);
        return NoContent();
    }

    // =============================================
    // ALERT STATUSES
    // =============================================

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

public record UpdateDeviceTypeRequest(
    string Name,
    string? Description,
    string? Icon,
    int SortOrder
);

public record UpdateDeviceStatusRequest(
    string Name,
    string? Description,
    string? Color,
    int SortOrder
);
public record CreateDeviceTypeRequest(
    string Name,
    string Code,
    string? Description,
    string? Icon,
    int SortOrder = 0
);

public record CreateDeviceStatusRequest(
    string Name,
    string Code,
    string? Description,
    string? Color,
    int SortOrder = 0
);

public record CreateAlertSeverityRequest(
    string Name,
    string Code,
    string? Description,
    string? Color,
    int Priority,
    int SortOrder = 0
);

public record UpdateAlertSeverityRequest(
    string Name,
    string? Description,
    string? Color,
    int Priority,
    int SortOrder
);