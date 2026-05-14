using EdgePulse.Domain.Common;

namespace EdgePulse.Domain.Entities;

public class LocationType : LookupBaseEntity
{
    protected LocationType() { }

    public static LocationType CreateSystemValue(
        Guid id, Guid templateId, string name, string code,
        string? description = null, int sortOrder = 0)
    {
        return new LocationType
        {
            Id = id, TemplateId = templateId, TenantId = null,
            Name = name, Code = code, Description = description,
            IsSystem = true, IsActive = true, SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
    }

    public static LocationType CreateCustomValue(
        Guid tenantId, string name, string code,
        string? description = null, int sortOrder = 0)
    {
        return new LocationType
        {
            Id = Guid.NewGuid(), TemplateId = null, TenantId = tenantId,
            Name = name, Code = code.ToUpperInvariant(),
            Description = description, IsSystem = false,
            IsActive = true, SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
    }
}
