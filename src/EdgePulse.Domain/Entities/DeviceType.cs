using EdgePulse.Domain.Common;

namespace EdgePulse.Domain.Entities;

public class DeviceType : LookupBaseEntity
{
    public string? Icon { get; private set; }

    protected DeviceType() { }

    // Factory method for template values (seeded)
    public static DeviceType CreateSystemValue(
        Guid id,
        Guid templateId,
        string name,
        string code,
        string? description = null,
        string? icon = null,
        int sortOrder = 0)
    {
        return new DeviceType
        {
            Id = id,
            TemplateId = templateId,
            TenantId = null,
            Name = name,
            Code = code,
            Description = description,
            Icon = icon,
            IsSystem = true,
            IsActive = true,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // Factory method for tenant custom values
    public static DeviceType CreateCustomValue(
        Guid tenantId,
        string name,
        string code,
        string? description = null,
        string? icon = null,
        int sortOrder = 0)
    {
        return new DeviceType
        {
            Id = Guid.NewGuid(),
            TemplateId = null,
            TenantId = tenantId,
            Name = name,
            Code = code.ToUpperInvariant(),
            Description = description,
            Icon = icon,
            IsSystem = false,
            IsActive = true,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
