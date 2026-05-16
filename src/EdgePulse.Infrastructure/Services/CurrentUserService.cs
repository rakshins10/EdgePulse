using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace EdgePulse.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserId => "dev-user-001";
    public string Email => "dev@edgepulse.com";
    public string FullName => "Dev User";
    public Guid TenantId => Guid.Parse("00000099-0000-0000-0000-000000000001");
    public UserRole Role => UserRole.SuperAdmin;
    public Guid? MillId => null;
    public IReadOnlyList<Guid> AreaIds => new List<Guid>();
    public bool IsAuthenticated => true;
    public bool IsSuperAdmin => true;
    public bool IsCustomerAdmin => false;
    public bool IsMillManager => false;
    public bool IsOperator => false;
    public bool IsExecutive => false;
}
