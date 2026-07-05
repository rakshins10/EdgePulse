using EdgePulse.Domain.Common;

namespace EdgePulse.Domain.Entities;

public class MetricType : LookupBaseEntity
{
    public string DefaultUnit { get; private set; } = string.Empty;
    // e.g. "C", "bar", "mm/s", "L/min", "kW"

    protected MetricType() { }

    public static MetricType CreateSystemValue(
        Guid id,
        Guid templateId,
        string name,
        string code,
        string defaultUnit,
        string? description = null,
        int sortOrder = 0)
    {
        return new MetricType
        {
            Id = id,
            TemplateId = templateId,
            TenantId = null,
            Name = name,
            Code = code,
            DefaultUnit = defaultUnit,
            Description = description,
            IsSystem = true,
            IsActive = true,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static MetricType CreateCustomValue(
        Guid tenantId,
        string name,
        string code,
        string defaultUnit,
        string? description = null,
        int sortOrder = 0)
    {
        return new MetricType
        {
            Id = Guid.NewGuid(),
            TemplateId = null,
            TenantId = tenantId,
            Name = name,
            Code = code.ToUpperInvariant(),
            DefaultUnit = defaultUnit,
            Description = description,
            IsSystem = false,
            IsActive = true,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
