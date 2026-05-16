using EdgePulse.Application.Features.Alerts.Queries;
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
    // DEVICE CONFIGURATION
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
    /// Get all device statuses for current tenant.
    /// Returns system defaults + tenant custom statuses.
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

    /// <summary>
    /// Get all metric types for current tenant.
    /// Returns system defaults + tenant custom metrics.
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
    // ALERT CONFIGURATION
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

    /// <summary>
    /// Get all alert statuses for current tenant.
    /// Includes IsTerminal flag (Resolved/Closed = no further transitions).
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
    /// SuperAdmin only -- used to assign templates to tenants.
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
