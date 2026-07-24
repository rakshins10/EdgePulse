namespace EdgePulse.Application.Common.Interfaces;

/// <summary>
/// A user as known by the identity provider (Keycloak). Role and scoping
/// live in user attributes and surface in JWT claims via protocol mappers.
/// </summary>
public record IdentityUser(
    string Id,
    string Username,
    string? Email,
    string? FirstName,
    string? LastName,
    bool Enabled,
    string? Role,
    Guid? TenantId,
    Guid? MillId,
    IReadOnlyList<Guid> AreaIds
);

public record CreateIdentityUser(
    string Email,
    string FirstName,
    string LastName,
    string Role,
    Guid TenantId,
    Guid? MillId,
    IReadOnlyList<Guid> AreaIds,
    string TemporaryPassword
);

/// <summary>
/// Administration of identity-provider users (list/create/update). The
/// Keycloak implementation lives in Infrastructure; handlers stay
/// provider-agnostic and enforce EdgePulse's authorization rules.
/// </summary>
public interface IIdentityAdminService
{
    Task<List<IdentityUser>> GetUsersAsync(CancellationToken cancellationToken);

    /// <summary>Creates the user and returns the new identity id.</summary>
    Task<string> CreateUserAsync(CreateIdentityUser user, CancellationToken cancellationToken);

    Task<IdentityUser?> GetUserAsync(string id, CancellationToken cancellationToken);

    /// <summary>Replace role / mill / area scoping attributes.</summary>
    Task UpdateUserRoleAsync(
        string id, string role, Guid? millId, IReadOnlyList<Guid> areaIds,
        CancellationToken cancellationToken);

    Task SetUserEnabledAsync(string id, bool enabled, CancellationToken cancellationToken);

    Task ResetPasswordAsync(
        string id, string temporaryPassword, CancellationToken cancellationToken);
}
