using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using ValidationException = EdgePulse.Application.Common.Exceptions.ValidationException;

namespace EdgePulse.Application.Features.Users;

// User administration is restricted to SuperAdmin and CustomerAdmin.
// CustomerAdmin operates within their own tenant only and cannot mint
// SuperAdmins. All handlers below share those rules via RequireUserAdmin.

public static class UserRoles
{
    public static readonly string[] All =
        ["SuperAdmin", "CustomerAdmin", "MillManager", "Operator", "Executive"];
}

internal static class UserAdminGuard
{
    public static void RequireUserAdmin(ICurrentUserService user)
    {
        if (!user.IsSuperAdmin && !user.IsCustomerAdmin)
            throw new ForbiddenAccessException();
    }

    public static void RequireCanManage(ICurrentUserService actor, IdentityUser target)
    {
        RequireUserAdmin(actor);
        if (actor.IsSuperAdmin) return;
        // CustomerAdmin: only users of their own tenant, never SuperAdmins
        if (target.Role == "SuperAdmin" || target.TenantId != actor.TenantId)
            throw new ForbiddenAccessException();
    }

    public static void RequireAssignableRole(ICurrentUserService actor, string role)
    {
        if (!UserRoles.All.Contains(role))
            throw new ValidationException(
                [new ValidationFailure("Role",
                    $"Unknown role '{role}'. Valid: {string.Join(", ", UserRoles.All)}")]);
        if (!actor.IsSuperAdmin && role == "SuperAdmin")
            throw new ForbiddenAccessException();
    }
}

// ── Queries ──────────────────────────────────────────────────────────────────

public record GetUsersQuery : IRequest<List<IdentityUser>>;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, List<IdentityUser>>
{
    private readonly IIdentityAdminService _identity;
    private readonly ICurrentUserService _currentUser;

    public GetUsersQueryHandler(IIdentityAdminService identity, ICurrentUserService currentUser)
    {
        _identity = identity;
        _currentUser = currentUser;
    }

    public async Task<List<IdentityUser>> Handle(
        GetUsersQuery request, CancellationToken cancellationToken)
    {
        UserAdminGuard.RequireUserAdmin(_currentUser);
        var users = await _identity.GetUsersAsync(cancellationToken);

        // CustomerAdmin sees only their own tenant's users
        if (!_currentUser.IsSuperAdmin)
            users = users
                .Where(u => u.TenantId == _currentUser.TenantId)
                .ToList();

        return users
            .OrderBy(u => u.Username, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

// ── Create ───────────────────────────────────────────────────────────────────

public record CreateUserCommand(
    string Email,
    string FirstName,
    string LastName,
    string Role,
    Guid? MillId,
    List<Guid> AreaIds,
    string TemporaryPassword
) : IRequest<string>;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Role).NotEmpty();
        RuleFor(x => x.TemporaryPassword).NotEmpty().MinimumLength(8).MaximumLength(100);
        RuleFor(x => x.MillId).NotEmpty()
            .When(x => x.Role == "MillManager")
            .WithMessage("MillManager requires a mill assignment.");
    }
}

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, string>
{
    private readonly IIdentityAdminService _identity;
    private readonly ICurrentUserService _currentUser;

    public CreateUserCommandHandler(IIdentityAdminService identity, ICurrentUserService currentUser)
    {
        _identity = identity;
        _currentUser = currentUser;
    }

    public async Task<string> Handle(
        CreateUserCommand request, CancellationToken cancellationToken)
    {
        UserAdminGuard.RequireUserAdmin(_currentUser);
        UserAdminGuard.RequireAssignableRole(_currentUser, request.Role);

        return await _identity.CreateUserAsync(
            new CreateIdentityUser(
                request.Email, request.FirstName, request.LastName,
                request.Role,
                _currentUser.TenantId, // users are created inside the actor's tenant
                request.MillId,
                request.AreaIds,
                request.TemporaryPassword),
            cancellationToken);
    }
}

// ── Update role / scoping ────────────────────────────────────────────────────

public record UpdateUserRoleCommand(
    string UserId,
    string Role,
    Guid? MillId,
    List<Guid> AreaIds
) : IRequest;

public class UpdateUserRoleCommandHandler : IRequestHandler<UpdateUserRoleCommand>
{
    private readonly IIdentityAdminService _identity;
    private readonly ICurrentUserService _currentUser;

    public UpdateUserRoleCommandHandler(IIdentityAdminService identity, ICurrentUserService currentUser)
    {
        _identity = identity;
        _currentUser = currentUser;
    }

    public async Task Handle(
        UpdateUserRoleCommand request, CancellationToken cancellationToken)
    {
        var target = await _identity.GetUserAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);
        UserAdminGuard.RequireCanManage(_currentUser, target);
        UserAdminGuard.RequireAssignableRole(_currentUser, request.Role);

        await _identity.UpdateUserRoleAsync(
            request.UserId, request.Role, request.MillId, request.AreaIds,
            cancellationToken);
    }
}

// ── Enable / disable ─────────────────────────────────────────────────────────

public record SetUserEnabledCommand(string UserId, bool Enabled) : IRequest;

public class SetUserEnabledCommandHandler : IRequestHandler<SetUserEnabledCommand>
{
    private readonly IIdentityAdminService _identity;
    private readonly ICurrentUserService _currentUser;

    public SetUserEnabledCommandHandler(IIdentityAdminService identity, ICurrentUserService currentUser)
    {
        _identity = identity;
        _currentUser = currentUser;
    }

    public async Task Handle(
        SetUserEnabledCommand request, CancellationToken cancellationToken)
    {
        var target = await _identity.GetUserAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);
        UserAdminGuard.RequireCanManage(_currentUser, target);

        // Nobody can disable themselves — avoids locking the last admin out.
        if (string.Equals(request.UserId, _currentUser.UserId, StringComparison.OrdinalIgnoreCase)
            && !request.Enabled)
            throw new ValidationException(
                [new ValidationFailure("Enabled", "You cannot disable your own account.")]);

        await _identity.SetUserEnabledAsync(request.UserId, request.Enabled, cancellationToken);
    }
}

// ── Reset password ───────────────────────────────────────────────────────────

public record ResetUserPasswordCommand(string UserId, string TemporaryPassword) : IRequest;

public class ResetUserPasswordCommandValidator : AbstractValidator<ResetUserPasswordCommand>
{
    public ResetUserPasswordCommandValidator()
    {
        RuleFor(x => x.TemporaryPassword).NotEmpty().MinimumLength(8).MaximumLength(100);
    }
}

public class ResetUserPasswordCommandHandler : IRequestHandler<ResetUserPasswordCommand>
{
    private readonly IIdentityAdminService _identity;
    private readonly ICurrentUserService _currentUser;

    public ResetUserPasswordCommandHandler(IIdentityAdminService identity, ICurrentUserService currentUser)
    {
        _identity = identity;
        _currentUser = currentUser;
    }

    public async Task Handle(
        ResetUserPasswordCommand request, CancellationToken cancellationToken)
    {
        var target = await _identity.GetUserAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);
        UserAdminGuard.RequireCanManage(_currentUser, target);

        await _identity.ResetPasswordAsync(
            request.UserId, request.TemporaryPassword, cancellationToken);
    }
}
