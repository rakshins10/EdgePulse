using EdgePulse.Domain.Common;

namespace EdgePulse.Domain.Entities;

public class DeviceStatus : LookupBaseEntity
{
    public string? Color { get; private set; }
    // Hex color for UI e.g. "#22c55e"

    protected DeviceStatus() { }

    public static DeviceStatus CreateSystemValue(
        Guid id,
        Guid templateId,
        string name,
        string code,
        string? color = null,
        string? description = null,
        int sortOrder = 0)
    {
        return new DeviceStatus
        {
            Id = id,
            TemplateId = templateId,
            TenantId = null,
            Name = name,
            Code = code,
            Color = color,
            Description = description,
            IsSystem = true,
            IsActive = true,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static DeviceStatus CreateCustomValue(
        Guid tenantId,
        string name,
        string code,
        string? color = null,
        string? description = null,
        int sortOrder = 0)
    {
        return new DeviceStatus
        {
            Id = Guid.NewGuid(),
            TemplateId = null,
            TenantId = tenantId,
            Name = name,
            Code = code.ToUpperInvariant(),
            Color = color,
            Description = description,
            IsSystem = false,
            IsActive = true,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
