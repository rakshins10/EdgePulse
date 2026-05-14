using EdgePulse.Domain.Common;

namespace EdgePulse.Domain.Entities;

public class Unit : LookupBaseEntity
{
    public string Symbol { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;

    protected Unit() { }

    public static Unit CreateSystemValue(
        Guid id, Guid templateId, string name, string code,
        string symbol, string category,
        string? description = null, int sortOrder = 0)
    {
        return new Unit
        {
            Id = id, TemplateId = templateId, TenantId = null,
            Name = name, Code = code, Symbol = symbol,
            Category = category, Description = description,
            IsSystem = true, IsActive = true, SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
    }

    public static Unit CreateCustomValue(
        Guid tenantId, string name, string code,
        string symbol, string category,
        string? description = null, int sortOrder = 0)
    {
        return new Unit
        {
            Id = Guid.NewGuid(), TemplateId = null, TenantId = tenantId,
            Name = name, Code = code.ToUpperInvariant(),
            Symbol = symbol, Category = category,
            Description = description, IsSystem = false,
            IsActive = true, SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
    }
}
