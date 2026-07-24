using EdgePulse.Domain.Common;

namespace EdgePulse.Domain.Entities;

/// <summary>
/// Per-tenant white-label branding: product name shown in the UI shell,
/// a logo URL and an accent colour. One row per tenant; absence means
/// EdgePulse defaults.
/// </summary>
public class TenantBranding : TenantBaseEntity
{
    public string ProductName { get; private set; } = "EdgePulse";
    public string? LogoUrl { get; private set; }
    public string? AccentColor { get; private set; }   // #rrggbb

    protected TenantBranding() { }

    public static TenantBranding Create(
        Guid tenantId, string productName, string? logoUrl, string? accentColor)
    {
        return new TenantBranding
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductName = productName,
            LogoUrl = logoUrl,
            AccentColor = accentColor,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string productName, string? logoUrl, string? accentColor)
    {
        ProductName = productName;
        LogoUrl = logoUrl;
        AccentColor = accentColor;
        MarkAsUpdated();
    }
}
