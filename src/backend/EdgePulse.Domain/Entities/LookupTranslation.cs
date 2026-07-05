using EdgePulse.Domain.Common;

namespace EdgePulse.Domain.Entities;

/// <summary>
/// A translated name/description for a single lookup item in a single locale.
/// Keyed by (LookupType, ItemId, LocaleCode). When no row exists for a
/// requested locale, the API falls back to the lookup item's stored English name.
///
/// LookupType is a stable string discriminator: "DeviceType", "DeviceStatus",
/// "LocationType", "MaintenanceType", "MetricType", "AlertSeverity".
/// </summary>
public class LookupTranslation : BaseEntity
{
    public string LookupType { get; private set; } = string.Empty;
    public Guid ItemId { get; private set; }
    public string LocaleCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    // Tenant-scoped: null = global/template translation, set = tenant-specific.
    public Guid? TenantId { get; private set; }

    protected LookupTranslation() { }

    public static LookupTranslation Create(
        string lookupType, Guid itemId, string localeCode,
        string name, string? description, Guid? tenantId)
    {
        return new LookupTranslation
        {
            Id = Guid.NewGuid(),
            LookupType = lookupType,
            ItemId = itemId,
            LocaleCode = localeCode.Trim().ToLowerInvariant(),
            Name = name,
            Description = description,
            TenantId = tenantId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
        MarkAsUpdated();
    }
}
