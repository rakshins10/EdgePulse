using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Application.Features.Ai;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdgePulse.API.Controllers;

/// <summary>
/// AI assistant features. Everything here degrades gracefully: if no provider
/// is configured or the model is unreachable, responses say so — they never
/// error. The rest of the platform does not depend on this controller.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAiAssistant _ai;

    public AiController(IMediator mediator, IAiAssistant ai)
    {
        _mediator = mediator;
        _ai = ai;
    }

    /// <summary>
    /// Is AI available on this deployment, and which provider/model?
    /// The dashboard uses this to show/hide AI controls.
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(AiStatusDto), 200)]
    public IActionResult GetStatus()
        => Ok(new AiStatusDto(_ai.IsEnabled, _ai.Description));

    /// <summary>
    /// Plain-language summary of an alert: what happened, likely causes,
    /// recommended action. Generated on first request and cached on the alert;
    /// pass regenerate=true to ask the model again.
    /// </summary>
    [HttpGet("alerts/{alertId:guid}/summary")]
    [ProducesResponseType(typeof(AlertSummaryResult), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetAlertSummary(
        Guid alertId,
        [FromQuery] bool regenerate = false,
        CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(new GetAlertSummaryQuery(alertId, regenerate), cancellationToken));

    /// <summary>
    /// Ask a natural-language question about the plant ("Which pumps alerted
    /// this week?", "What is wrong with PUMP-LW-001?"). The answer is grounded
    /// in live device / alert / work-order data the caller is allowed to see.
    /// Optionally pass deviceId to focus on one device. Nothing is stored.
    /// </summary>
    [HttpPost("ask")]
    [ProducesResponseType(typeof(AskResult), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Ask(
        [FromBody] AskRequest request,
        CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(new AskQuestionQuery(request.Question, request.DeviceId), cancellationToken));
}

public record AiStatusDto(bool Enabled, string Provider);

/// <summary>Body for POST /api/ai/ask.</summary>
public record AskRequest(string Question, Guid? DeviceId = null);
