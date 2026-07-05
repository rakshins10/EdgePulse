using EdgePulse.Domain.Common;

namespace EdgePulse.Domain.Entities;

public class TenantLookupOverride : BaseEntity
{
    public Guid TenantId { get; private set; }
    public string LookupType { get; private set; } = string.Empty;
    public Guid LookupId { get; private set; }
    public string? DisplayName { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string UpdatedBy { get; private set; } = string.Empty;

    protected TenantLookupOverride() { }

    public static TenantLookupOverride Create(
        Guid tenantId, string lookupType, Guid lookupId,
        string updatedBy, string? displayName = null, bool isActive = true)
    {
        return new TenantLookupOverride
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            LookupType = lookupType, LookupId = lookupId,
            DisplayName = displayName, IsActive = isActive,
            UpdatedBy = updatedBy,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
    }

    public void Rename(string displayName, string updatedBy)
    {
        DisplayName = displayName;
        UpdatedBy = updatedBy;
        MarkAsUpdated();
    }

    public void Deactivate(string updatedBy)
    {
        IsActive = false;
        UpdatedBy = updatedBy;
        MarkAsUpdated();
    }

    public void Reactivate(string updatedBy)
    {
        IsActive = true;
        UpdatedBy = updatedBy;
        MarkAsUpdated();
    }
}
