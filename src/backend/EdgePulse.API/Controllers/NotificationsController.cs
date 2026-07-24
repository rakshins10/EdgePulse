using EdgePulse.Application.Features.Notifications.Commands;
using EdgePulse.Application.Features.Notifications.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdgePulse.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get recent notifications for the current tenant (newest first).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<NotificationDto>), 200)]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetNotificationsQuery(unreadOnly, take), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Number of unread notifications — drives the bell badge.
    /// </summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(int), 200)]
    public async Task<IActionResult> GetUnreadCount(
        CancellationToken cancellationToken = default)
    {
        var count = await _mediator.Send(
            new GetUnreadNotificationCountQuery(), cancellationToken);
        return Ok(count);
    }

    /// <summary>
    /// Mark one notification as read.
    /// </summary>
    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> MarkRead(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await _mediator.Send(
            new MarkNotificationReadCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Mark every unread notification as read.
    /// </summary>
    [HttpPost("read-all")]
    [ProducesResponseType(typeof(int), 200)]
    public async Task<IActionResult> MarkAllRead(
        CancellationToken cancellationToken = default)
    {
        var count = await _mediator.Send(
            new MarkAllNotificationsReadCommand(), cancellationToken);
        return Ok(count);
    }
}
