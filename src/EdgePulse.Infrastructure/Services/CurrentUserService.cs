using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace EdgePulse.Infrastructure.Services;

/// <summary>
/// Reads the current user''s identity from the Keycloak JWT claims injected by
/// the JwtBearer middleware. Claim names match the User Attribute protocol mappers
/// configured in the edgepulse-api client scope:
///   sub        -> UserId
///   email      -> Email
///   name       -> FullName  (optional)
///   tenantId   -> TenantId  (custom User Attribute mapper)
///   role       -> Role      (custom User Attribute mapper)
///   millId     -> MillId    (custom User Attribute mapper, optional)
///   areaIds    -> AreaIds   (custom User Attribute mapper, Multivalued=ON, optional)
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User =>
        _httpContextAccessor.HttpContext?.User;

    private string? Claim(string type) =>
        User?.FindFirst(type)?.Value;

    // ----------------------------------------------------------------
    // Core identity
    // ----------------------------------------------------------------

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated ?? false;

    // MapInboundClaims = false in Program.cs means JWT claim names are preserved as-is.
    // "sub" stays "sub", "role" stays "role", "email" stays "email".
    public string UserId =>
        Claim("sub")
        ?? string.Empty;

    public string Email =>
        Claim("email")
        ?? string.Empty;

    public string FullName =>
        Claim("name")
        ?? Claim(ClaimTypes.Name)
        ?? Email;

    // ----------------------------------------------------------------
    // EdgePulse custom claims (set via User Attribute protocol mappers)
    // ----------------------------------------------------------------

    public Guid TenantId
    {
        get
        {
            var raw = Claim("tenantId");
            return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
        }
    }

    public UserRole Role
    {
        get
        {
            var raw = Claim("role");
            return Enum.TryParse<UserRole>(raw, ignoreCase: true, out var role)
                ? role
                : UserRole.Operator; // safest default — least privileged
        }
    }

    public Guid? MillId
    {
        get
        {
            var raw = Claim("millId");
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public IReadOnlyList<Guid> AreaIds
    {
        get
        {
            // Multivalued mapper emits multiple claims with the same name
            var claims = User?.FindAll("areaIds") ?? Enumerable.Empty<Claim>();
            return claims
                .Select(c => Guid.TryParse(c.Value, out var id) ? (Guid?)id : null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList()
                .AsReadOnly();
        }
    }

    // ----------------------------------------------------------------
    // Role helpers (used in command handlers for authorization checks)
    // ----------------------------------------------------------------

    public bool IsSuperAdmin    => Role == UserRole.SuperAdmin;
    public bool IsCustomerAdmin => Role == UserRole.CustomerAdmin;
    public bool IsMillManager   => Role == UserRole.MillManager;
    public bool IsOperator      => Role == UserRole.Operator;
    public bool IsExecutive     => Role == UserRole.Executive;
}
