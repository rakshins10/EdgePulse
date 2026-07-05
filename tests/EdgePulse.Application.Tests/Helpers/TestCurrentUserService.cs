using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Enums;

namespace EdgePulse.Application.Tests.Helpers;

/// <summary>
/// Configurable stub for ICurrentUserService.
/// </summary>
public class TestCurrentUserService : ICurrentUserService
{
    public string UserId { get; set; } = "test-user-id";
    public string Email { get; set; } = "test@edgepulse.io";
    public string FullName { get; set; } = "Test User";
    public Guid TenantId { get; set; } = Guid.NewGuid();
    public UserRole Role { get; set; } = UserRole.CustomerAdmin;
    public Guid? MillId { get; set; } = null;
    public IReadOnlyList<Guid> AreaIds { get; set; } = Array.Empty<Guid>();
    public bool IsAuthenticated { get; set; } = true;
    public bool IsSuperAdmin     => Role == UserRole.SuperAdmin;
    public bool IsCustomerAdmin  => Role == UserRole.CustomerAdmin;
    public bool IsMillManager    => Role == UserRole.MillManager;
    public bool IsOperator       => Role == UserRole.Operator;
    public bool IsExecutive      => Role == UserRole.Executive;

    public static TestCurrentUserService AsCustomerAdmin(Guid? tenantId = null) =>
        new() { Role = UserRole.CustomerAdmin, TenantId = tenantId ?? Guid.NewGuid() };

    public static TestCurrentUserService AsSuperAdmin() =>
        new() { Role = UserRole.SuperAdmin };

    public static TestCurrentUserService AsOperator(Guid? tenantId = null) =>
        new() { Role = UserRole.Operator, TenantId = tenantId ?? Guid.NewGuid() };

    public static TestCurrentUserService AsExecutive(Guid? tenantId = null) =>
        new() { Role = UserRole.Executive, TenantId = tenantId ?? Guid.NewGuid() };
}
