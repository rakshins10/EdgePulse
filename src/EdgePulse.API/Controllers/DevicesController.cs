using EdgePulse.Application.Features.Devices.Commands;
using EdgePulse.Application.Features.Devices.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EdgePulse.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public DevicesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all devices for current tenant.
    /// Optionally filter by mill or area.
    /// Role-scoped: MillManager sees their mill,
    /// Operator sees assigned areas only.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<DeviceListDto>), 200)]
    public async Task<IActionResult> GetDevices(
        [FromQuery] Guid? millId,
        [FromQuery] Guid? areaId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetDevicesQuery(areaId, millId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Register a new device.
    /// Returns device ID and API key.
    /// API KEY IS SHOWN ONLY ONCE -- store it securely.
    /// MillManager can only register in their assigned mill.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(RegisterDeviceResult), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> RegisterDevice(
        [FromBody] RegisterDeviceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RegisterDeviceCommand(
                request.AreaId,
                request.TypeId,
                request.StatusId,
                request.Name,
                request.Code,
                request.ManufacturerId,
                request.ModelId,
                request.SerialNumber,
                request.InstallDate,
                request.Description),
            cancellationToken);
        return CreatedAtAction(nameof(GetDevices), new { }, result);
    }
}

public record RegisterDeviceRequest(
    Guid AreaId,
    Guid TypeId,
    Guid StatusId,
    string Name,
    string Code,
    Guid? ManufacturerId = null,
    Guid? ModelId = null,
    string? SerialNumber = null,
    DateOnly? InstallDate = null,
    string? Description = null
);
