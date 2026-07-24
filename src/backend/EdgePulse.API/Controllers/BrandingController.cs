using EdgePulse.Application.Features.Branding;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdgePulse.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class BrandingController : ControllerBase
{
    private readonly IMediator _mediator;

    public BrandingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Tenant branding (EdgePulse defaults when unset).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(BrandingDto), 200)]
    public async Task<IActionResult> GetBranding(CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(new GetBrandingQuery(), cancellationToken));

    /// <summary>Update tenant branding (admins only).</summary>
    [HttpPut]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> UpdateBranding(
        [FromBody] UpdateBrandingRequest request,
        CancellationToken cancellationToken = default)
    {
        await _mediator.Send(
            new UpdateBrandingCommand(
                request.ProductName, request.LogoUrl, request.AccentColor),
            cancellationToken);
        return NoContent();
    }
}

public record UpdateBrandingRequest(string ProductName, string? LogoUrl, string? AccentColor);
