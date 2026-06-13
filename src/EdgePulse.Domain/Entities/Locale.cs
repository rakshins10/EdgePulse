using EdgePulse.Domain.Common;

namespace EdgePulse.Domain.Entities;

/// <summary>
/// A supported UI/content language. Data-driven so new languages can be added
/// at runtime without a code change. The <see cref="Code"/> is a BCP-47 tag
/// (e.g. "en", "fi", "sv", "de").
/// </summary>
public class Locale : BaseEntity
{
    public string Code { get; private set; } = string.Empty;          // "en", "fi"
    public string DisplayName { get; private set; } = string.Empty;   // "English" (English label)
    public string NativeName { get; private set; } = string.Empty;    // "Suomi"
    public string? Flag { get; private set; }                         // optional emoji/icon
    public bool IsEnabled { get; private set; } = true;
    public bool IsDefault { get; private set; }
    public int SortOrder { get; private set; }

    protected Locale() { }

    public static Locale Create(
        string code, string displayName, string nativeName,
        string? flag = null, bool isEnabled = true,
        bool isDefault = false, int sortOrder = 0)
    {
        return new Locale
        {
            Id = Guid.NewGuid(),
            Code = code.Trim().ToLowerInvariant(),
            DisplayName = displayName,
            NativeName = nativeName,
            Flag = flag,
            IsEnabled = isEnabled,
            IsDefault = isDefault,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public void UpdateDetails(
        string displayName, string nativeName, string? flag,
        bool isEnabled, int sortOrder)
    {
        DisplayName = displayName;
        NativeName = nativeName;
        Flag = flag;
        IsEnabled = isEnabled;
        SortOrder = sortOrder;
        MarkAsUpdated();
    }

    public void SetAsDefault()
    {
        IsDefault = true;
        IsEnabled = true; // the default locale must be enabled
        MarkAsUpdated();
    }

    public void ClearDefault()
    {
        IsDefault = false;
        MarkAsUpdated();
    }
}
