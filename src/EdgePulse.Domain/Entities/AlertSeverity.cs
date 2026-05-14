using EdgePulse.Domain.Common;

namespace EdgePulse.Domain.Entities;

public class AlertSeverity : LookupBaseEntity
{
    public string? Color { get; private set; }
    public int Priority { get; private set; }
    // Lower number = higher priority
    // Critical = 1, High = 2, Medium = 3, Low = 4

    protected AlertSeverity() { }

    public static AlertSeverity CreateSystemValue(
        Guid id,
        Guid templateId,
        string name,
        string code,
        int priority,
        string? color = null,
        string? description = null,
        int sortOrder = 0)
    {
        return new AlertSeverity
        {
            Id = id,
            TemplateId = templateId,
            TenantId = null,
            Name = name,
            Code = code,
            Priority = priority,
            Color = color,
            Description = description,
            IsSystem = true,
            IsActive = true,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static AlertSeverity CreateCustomValue(
        Guid tenantId,
        string name,
        string code,
        int priority,
        string? color = null,
        string? description = null,
        int sortOrder = 0)
    {
        return new AlertSeverity
        {
            Id = Guid.NewGuid(),
            TemplateId = null,
            TenantId = tenantId,
            Name = name,
            Code = code.ToUpperInvariant(),
            Priority = priority,
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
