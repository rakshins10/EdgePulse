using EdgePulse.Domain.Common;
using EdgePulse.Domain.Enums;

namespace EdgePulse.Domain.Entities;

public class Mill : TenantBaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string Location { get; private set; } = string.Empty;
    public string Timezone { get; private set; } = string.Empty;
    public bool HasInternet { get; private set; } = true;
    public DeploymentMode DeploymentMode { get; private set; }
        = DeploymentMode.Cloud;
    public Tenant? Tenant { get; private set; }
    public ICollection<Area> Areas { get; private set; }
        = new List<Area>();

    protected Mill() { }

    public static Mill Create(
        Guid tenantId, string name, string code,
        string location, string timezone,
        bool hasInternet = true,
        DeploymentMode deploymentMode = DeploymentMode.Cloud)
    {
        return new Mill
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            Name = name, Code = code.ToUpperInvariant(),
            Location = location, Timezone = timezone,
            HasInternet = hasInternet, DeploymentMode = deploymentMode,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateDetails(string name, string location,
        string timezone, bool hasInternet,
        DeploymentMode deploymentMode)
    {
        Name = name; Location = location; Timezone = timezone;
        HasInternet = hasInternet; DeploymentMode = deploymentMode;
        MarkAsUpdated();
    }
}
