using EdgePulse.Domain.Common;

namespace EdgePulse.Domain.Entities;

/// <summary>
/// A tenant-scoped override for a single UI chrome string in a single locale.
/// The canonical key registry and English source live in the frontend's
/// en.json; this table only stores translated/overridden values that are
/// layered on top of the bundled JSON at runtime.
///
/// Key is the i18next dot-path, e.g. "mills.addMill", "nav.devices".
/// </summary>
public class UiStringTranslation : BaseEntity
{
    public string LocaleCode { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public Guid? TenantId { get; private set; }   // null = global, set = tenant override

    protected UiStringTranslation() { }

    public static UiStringTranslation Create(
        string localeCode, string key, string value, Guid? tenantId)
    {
        return new UiStringTranslation
        {
            Id = Guid.NewGuid(),
            LocaleCode = localeCode.Trim().ToLowerInvariant(),
            Key = key.Trim(),
            Value = value,
            TenantId = tenantId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public void Update(string value)
    {
        Value = value;
        MarkAsUpdated();
    }
}
