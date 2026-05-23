using EdgePulse.Application.Features.Areas.Commands;
using EdgePulse.Application.Features.Areas.Queries;
using EdgePulse.Application.Features.Mills.Commands;
using EdgePulse.Application.Features.Mills.Queries;
using EdgePulse.Application.Features.Tenants.Commands;
using EdgePulse.Application.Features.Tenants.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdgePulse.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class OrganisationController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrganisationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // =============================================
    // TENANTS (SuperAdmin only)
    // =============================================

    /// <summary>
    /// Get all tenants. SuperAdmin only.
    /// </summary>
    [HttpGet("tenants")]
    [ProducesResponseType(typeof(List<TenantDto>), 200)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetTenants(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetTenantsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a new tenant. SuperAdmin only.
    /// Optionally assign an industry template.
    /// </summary>
    [HttpPost("tenants")]
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> CreateTenant(
        [FromBody] CreateTenantRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            new CreateTenantCommand(
                request.Name,
                request.Slug,
                request.ContactEmail,
                request.TemplateId),
            cancellationToken);
        return CreatedAtAction(nameof(GetTenants), new { }, id);
    }

    // =============================================
    // MILLS
    // =============================================

    /// <summary>
    /// Get all mills for current tenant.
    /// SuperAdmin sees all. MillManager sees their mill only.
    /// </summary>
    [HttpGet("mills")]
    [ProducesResponseType(typeof(List<MillDto>), 200)]
    public async Task<IActionResult> GetMills(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetMillsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a new mill. CustomerAdmin and above.
    /// </summary>
    [HttpPost("mills")]
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> CreateMill(
        [FromBody] CreateMillRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            new CreateMillCommand(
                request.Name,
                request.Code,
                request.Location,
                request.Timezone,
                request.HasInternet,
                request.DeploymentMode),
            cancellationToken);
        return CreatedAtAction(nameof(GetMills), new { }, id);
    }

    // =============================================
    // AREAS
    // =============================================

    /// <summary>
    /// Get all areas. Optionally filter by mill.
    /// Role-based: MillManager sees their mill, Operator sees assigned areas.
    /// </summary>
    [HttpGet("areas")]
    [ProducesResponseType(typeof(List<AreaDto>), 200)]
    public async Task<IActionResult> GetAreas(
        [FromQuery] Guid? millId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetAreasQuery(millId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a new area within a mill.
    /// MillManager can only create areas in their assigned mill.
    /// </summary>
    [HttpPost("areas")]
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> CreateArea(
        [FromBody] CreateAreaRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            new CreateAreaCommand(
                request.MillId,
                request.Name,
                request.Code,
                request.LocationTypeId,
                request.Description),
            cancellationToken);
        return CreatedAtAction(nameof(GetAreas), new { }, id);
    }
}

// Request models
public record CreateTenantRequest(
    string Name,
    string Slug,
    string ContactEmail,
    Guid? TemplateId = null
);

public record CreateMillRequest(
    string Name,
    string Code,
    string Location,
    string Timezone,
    bool HasInternet = true,
    EdgePulse.Domain.Enums.DeploymentMode DeploymentMode =
        EdgePulse.Domain.Enums.DeploymentMode.Cloud
);

public record CreateAreaRequest(
    Guid MillId,
    string Name,
    string Code,
    Guid? LocationTypeId = null,
    string? Description = null
);
