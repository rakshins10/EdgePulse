using EdgePulse.Application.Features.Alerts.Queries;
using EdgePulse.Application.Features.Devices.Commands;
using EdgePulse.Application.Features.Alerts.Commands;
using EdgePulse.Application.Features.Devices.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdgePulse.API.Controllers;

[ApiController]
[Authorize]
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
    // MANUFACTURERS
    // =============================================

    /// <summary>
    /// Get all device manufacturers for current tenant.
    /// </summary>
    [HttpGet("manufacturers")]
    [ProducesResponseType(typeof(List<DeviceManufacturerDto>), 200)]
    public async Task<IActionResult> GetManufacturers(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetDeviceManufacturersQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a custom device manufacturer.
    /// </summary>
    [HttpPost("manufacturers")]
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> CreateManufacturer(
        [FromBody] CreateManufacturerRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            new CreateDeviceManufacturerCommand(
                request.Name, request.Code,
                request.Website, request.Country,
                request.SortOrder),
            cancellationToken);
        return CreatedAtAction(nameof(GetManufacturers), new { }, id);
    }

    // =============================================
    // DEVICE MODELS
    // =============================================

    /// <summary>
    /// Create a custom device model under a manufacturer.
    /// </summary>
    [HttpPost("device-models")]
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> CreateDeviceModel(
        [FromBody] CreateDeviceModelRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            new CreateDeviceModelCommand(
                request.ManufacturerId, request.Name, request.Code,
                request.ModelNumber, request.Specifications,
                request.SortOrder),
            cancellationToken);
        return CreatedAtAction(nameof(GetManufacturers), new { }, id);
    }

    // =============================================
    // MAINTENANCE TYPES
    // =============================================

    /// <summary>
    /// Get all maintenance types for current tenant.
    /// </summary>
    [HttpGet("maintenance-types")]
    [ProducesResponseType(typeof(List<MaintenanceTypeDto>), 200)]
    public async Task<IActionResult> GetMaintenanceTypes(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetMaintenanceTypesQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a custom maintenance type.
    /// </summary>
    [HttpPost("maintenance-types")]
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> CreateMaintenanceType(
        [FromBody] CreateMaintenanceTypeRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            new CreateMaintenanceTypeCommand(
                request.Name, request.Code,
                request.Description, request.Color,
                request.SortOrder),
            cancellationToken);
        return CreatedAtAction(nameof(GetMaintenanceTypes), new { }, id);
    }

    /// <summary>
    /// Update a custom maintenance type (name, description, colour).
    /// </summary>
    [HttpPut("maintenance-types/{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateMaintenanceType(
        Guid id,
        [FromBody] UpdateMaintenanceTypeRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpdateMaintenanceTypeCommand(
                id, request.Name, request.Description,
                request.Color, request.SortOrder),
            cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Delete (deactivate) a custom maintenance type.
    /// </summary>
    [HttpDelete("maintenance-types/{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteMaintenanceType(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new DeleteMaintenanceTypeCommand(id), cancellationToken);
        return NoContent();
    }

    // =============================================
    // LOCATION TYPES
    // =============================================

    /// <summary>
    /// Get all location types for current tenant.
    /// </summary>
    [HttpGet("location-types")]
    [ProducesResponseType(typeof(List<LocationTypeDto>), 200)]
    public async Task<IActionResult> GetLocationTypes(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetLocationTypesQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a custom location type.
    /// </summary>
    [HttpPost("location-types")]
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> CreateLocationType(
        [FromBody] CreateLocationTypeRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            new CreateLocationTypeCommand(
                request.Name, request.Code,
                request.Description, request.SortOrder),
            cancellationToken);
        return CreatedAtAction(nameof(GetLocationTypes), new { }, id);
    }

    // =============================================
    // METRIC TYPES -- POST
    // =============================================

    /// <summary>
    /// Create a custom metric type.
    /// </summary>
    [HttpPost("metric-types")]
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> CreateMetricType(
        [FromBody] CreateMetricTypeRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            new CreateMetricTypeCommand(
                request.Name, request.Code,
                request.DefaultUnit, request.Description,
                request.SortOrder),
            cancellationToken);
        return CreatedAtAction(nameof(GetMetricTypes), new { }, id);
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

    // =============================================
    // TENANT LOOKUP OVERRIDES
    // =============================================

    /// <summary>
    /// Get all tenant lookup overrides.
    /// Shows which template values have been renamed or disabled.
    /// Optionally filter by lookup type (e.g. DeviceType, DeviceStatus).
    /// </summary>
    [HttpGet("lookup-overrides")]
    [ProducesResponseType(typeof(List<TenantLookupOverrideDto>), 200)]
    public async Task<IActionResult> GetLookupOverrides(
        [FromQuery] string? lookupType,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetTenantLookupOverridesQuery(lookupType),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create or update a tenant lookup override.
    /// Use this to rename or disable a template lookup value.
    /// If override already exists it will be updated.
    /// </summary>
    [HttpPut("lookup-overrides")]
    [ProducesResponseType(typeof(Guid), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> UpsertLookupOverride(
        [FromBody] UpsertLookupOverrideRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            new UpsertTenantLookupOverrideCommand(
                request.LookupType,
                request.LookupId,
                request.DisplayName,
                request.IsActive),
            cancellationToken);
        return Ok(id);
    }

    /// <summary>
    /// Delete a tenant lookup override.
    /// Restores the template default name and active state.
    /// </summary>
    [HttpDelete("lookup-overrides/{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteLookupOverride(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new DeleteTenantLookupOverrideCommand(id),
            cancellationToken);
        return NoContent();
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

public record CreateManufacturerRequest(
    string Name,
    string Code,
    string? Website,
    string? Country,
    int SortOrder = 0
);

public record CreateDeviceModelRequest(
    Guid ManufacturerId,
    string Name,
    string Code,
    string? ModelNumber,
    string? Specifications,
    int SortOrder = 0
);

public record CreateMaintenanceTypeRequest(
    string Name,
    string Code,
    string? Description,
    string? Color,
    int SortOrder = 0
);

public record UpdateMaintenanceTypeRequest(
    string Name,
    string? Description,
    string? Color,
    int SortOrder = 0
);

public record CreateLocationTypeRequest(
    string Name,
    string Code,
    string? Description,
    int SortOrder = 0
);

public record CreateMetricTypeRequest(
    string Name,
    string Code,
    string DefaultUnit,
    string? Description,
    int SortOrder = 0
);

public record UpsertLookupOverrideRequest(
    string LookupType,
    Guid LookupId,
    string? DisplayName,
    bool IsActive = true
);