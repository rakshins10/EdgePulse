using EdgePulse.Domain.Common;

namespace EdgePulse.Domain.Entities;

public class AlertStatus : LookupBaseEntity
{
    public bool IsTerminal { get; private set; }
    // Terminal = no further state transitions allowed
    // Resolved and Closed are terminal states

    protected AlertStatus() { }

    public static AlertStatus CreateSystemValue(
        Guid id,
        Guid templateId,
        string name,
        string code,
        bool isTerminal = false,
        string? description = null,
        int sortOrder = 0)
    {
        return new AlertStatus
        {
            Id = id,
            TemplateId = templateId,
            TenantId = null,
            Name = name,
            Code = code,
            IsTerminal = isTerminal,
            Description = description,
            IsSystem = true,
            IsActive = true,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static AlertStatus CreateCustomValue(
        Guid tenantId,
        string name,
        string code,
        bool isTerminal = false,
        string? description = null,
        int sortOrder = 0)
    {
        return new AlertStatus
        {
            Id = Guid.NewGuid(),
            TemplateId = null,
            TenantId = tenantId,
            Name = name,
            Code = code.ToUpperInvariant(),
            IsTerminal = isTerminal,
            Description = description,
            IsSystem = false,
            IsActive = true,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
