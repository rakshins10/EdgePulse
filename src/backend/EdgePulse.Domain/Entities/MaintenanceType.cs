using EdgePulse.Domain.Common;

namespace EdgePulse.Domain.Entities;

public class MaintenanceType : LookupBaseEntity
{
    public string? Color { get; private set; }

    protected MaintenanceType() { }

    public static MaintenanceType CreateSystemValue(
        Guid id, Guid templateId, string name, string code,
        string? color = null, string? description = null, int sortOrder = 0)
    {
        return new MaintenanceType
        {
            Id = id, TemplateId = templateId, TenantId = null,
            Name = name, Code = code, Color = color,
            Description = description, IsSystem = true,
            IsActive = true, SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
    }

    public static MaintenanceType CreateCustomValue(
        Guid tenantId, string name, string code,
        string? color = null, string? description = null, int sortOrder = 0)
    {
        return new MaintenanceType
        {
            Id = Guid.NewGuid(), TemplateId = null, TenantId = tenantId,
            Name = name, Code = code.ToUpperInvariant(), Color = color,
            Description = description, IsSystem = false,
            IsActive = true, SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Update the display colour. System maintenance types are protected —
    /// tenants customise them via a TenantLookupOverride instead.
    /// </summary>
    public void UpdateColor(string? color)
    {
        if (IsSystem)
            throw new InvalidOperationException(
                "System maintenance types cannot be modified directly. " +
                "Use TenantLookupOverride instead.");
        Color = color;
        MarkAsUpdated();
    }
}
