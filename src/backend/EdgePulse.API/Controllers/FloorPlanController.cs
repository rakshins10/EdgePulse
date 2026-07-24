using EdgePulse.Application.Features.FloorPlan;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdgePulse.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class FloorPlanController : ControllerBase
{
    private readonly IMediator _mediator;

    public FloorPlanController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Devices of a mill with floor positions, live status and alert counts.
    /// </summary>
    [HttpGet("{millId:guid}")]
    [ProducesResponseType(typeof(List<FloorPlanDeviceDto>), 200)]
    public async Task<IActionResult> GetFloorPlan(
        Guid millId, CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(new GetFloorPlanQuery(millId), cancellationToken));

    /// <summary>
    /// Place a device on the plan (percent coordinates) or clear with nulls.
    /// </summary>
    [HttpPut("devices/{deviceId:guid}/position")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> SetPosition(
        Guid deviceId,
        [FromBody] SetPositionRequest request,
        CancellationToken cancellationToken = default)
    {
        await _mediator.Send(
            new SetDevicePositionCommand(deviceId, request.X, request.Y),
            cancellationToken);
        return NoContent();
    }
}

public record SetPositionRequest(double? X, double? Y);
