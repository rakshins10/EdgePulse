using EdgePulse.Domain.Enums;

namespace EdgePulse.Application.Common.Interfaces;

public interface ICurrentUserService
{
    string UserId { get; }
    string Email { get; }
    string FullName { get; }
    Guid TenantId { get; }
    UserRole Role { get; }
    Guid? MillId { get; }
    // null for SuperAdmin and CustomerAdmin
    IReadOnlyList<Guid> AreaIds { get; }
    // empty for non-operators
    bool IsAuthenticated { get; }
    bool IsSuperAdmin { get; }
    bool IsCustomerAdmin { get; }
    bool IsMillManager { get; }
    bool IsOperator { get; }
    bool IsExecutive { get; }
}
