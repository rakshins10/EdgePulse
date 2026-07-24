using EdgePulse.Application.Features.Webhooks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdgePulse.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class WebhooksController : ControllerBase
{
    private readonly IMediator _mediator;

    public WebhooksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>List webhook subscriptions (admin only).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<WebhookDto>), 200)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetWebhooks(CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(new GetWebhooksQuery(), cancellationToken));

    /// <summary>Available event keys.</summary>
    [HttpGet("events")]
    [ProducesResponseType(typeof(string[]), 200)]
    public IActionResult GetEvents() => Ok(WebhookEvents.All);

    /// <summary>Create a subscription.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Create(
        [FromBody] CreateWebhookRequest request,
        CancellationToken cancellationToken = default)
    {
        var id = await _mediator.Send(
            new CreateWebhookCommand(
                request.Name, request.Url, request.Secret,
                request.Events, request.Format ?? "json"),
            cancellationToken);
        return CreatedAtAction(nameof(GetWebhooks), new { }, id);
    }

    /// <summary>Update a subscription (empty secret keeps the existing one).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateWebhookRequest request,
        CancellationToken cancellationToken = default)
    {
        await _mediator.Send(
            new UpdateWebhookCommand(
                id, request.Name, request.Url, request.Secret,
                request.Events, request.Format ?? "json", request.IsActive),
            cancellationToken);
        return NoContent();
    }

    /// <summary>Delete a subscription.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(
        Guid id, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new DeleteWebhookCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Send a signed test payload; returns the delivery status.</summary>
    [HttpPost("{id:guid}/test")]
    [ProducesResponseType(typeof(string), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Test(
        Guid id, CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(new TestWebhookCommand(id), cancellationToken));
}

public record CreateWebhookRequest(
    string Name, string Url, string Secret, List<string> Events, string? Format);

public record UpdateWebhookRequest(
    string Name, string Url, string? Secret, List<string> Events,
    string? Format, bool IsActive);
