using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Application.Features.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdgePulse.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// List users. SuperAdmin sees everyone; CustomerAdmin their tenant.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<IdentityUser>), 200)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken = default)
    {
        var users = await _mediator.Send(new GetUsersQuery(), cancellationToken);
        return Ok(users);
    }

    /// <summary>
    /// Create a user in the current tenant with a temporary password.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(string), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var id = await _mediator.Send(
            new CreateUserCommand(
                request.Email, request.FirstName, request.LastName,
                request.Role, request.MillId, request.AreaIds ?? [],
                request.TemporaryPassword),
            cancellationToken);
        return CreatedAtAction(nameof(GetUsers), new { }, id);
    }

    /// <summary>
    /// Change a user's role and mill/area scoping.
    /// </summary>
    [HttpPut("{id}/role")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateRole(
        string id,
        [FromBody] UpdateUserRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        await _mediator.Send(
            new UpdateUserRoleCommand(
                id, request.Role, request.MillId, request.AreaIds ?? []),
            cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Enable or disable a user account.
    /// </summary>
    [HttpPut("{id}/enabled")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> SetEnabled(
        string id,
        [FromBody] SetUserEnabledRequest request,
        CancellationToken cancellationToken = default)
    {
        await _mediator.Send(
            new SetUserEnabledCommand(id, request.Enabled), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Set a temporary password (user must change it at next login).
    /// </summary>
    [HttpPost("{id}/reset-password")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ResetPassword(
        string id,
        [FromBody] ResetUserPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        await _mediator.Send(
            new ResetUserPasswordCommand(id, request.TemporaryPassword),
            cancellationToken);
        return NoContent();
    }
}

public record CreateUserRequest(
    string Email,
    string FirstName,
    string LastName,
    string Role,
    Guid? MillId,
    List<Guid>? AreaIds,
    string TemporaryPassword
);

public record UpdateUserRoleRequest(
    string Role,
    Guid? MillId,
    List<Guid>? AreaIds
);

public record SetUserEnabledRequest(bool Enabled);

public record ResetUserPasswordRequest(string TemporaryPassword);
