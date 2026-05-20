using EdgePulse.Domain.Common;

namespace EdgePulse.Domain.Entities;

public class DeviceApiKey : BaseEntity
{
    public Guid TenantId { get; private set; }
    public Guid DeviceId { get; private set; }
    public string KeyHash { get; private set; } = string.Empty;
    // SHA-256 hash -- plain text never stored
    public string KeyPrefix { get; private set; } = string.Empty;
    // First 8 chars for display e.g. "dev_a1b2"
    public bool IsActive { get; private set; } = true;
    public DateTime? ExpiresAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? RevokedReason { get; private set; }

    // Navigation
    public Device? Device { get; private set; }

    protected DeviceApiKey() { }

    public static DeviceApiKey Create(
        Guid tenantId,
        Guid deviceId,
        string keyHash,
        string keyPrefix,
        DateTime? expiresAt = null)
    {
        return new DeviceApiKey
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DeviceId = deviceId,
            KeyHash = keyHash,
            KeyPrefix = keyPrefix,
            IsActive = true,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Revoke(string reason)
    {
        IsActive = false;
        RevokedAt = DateTime.UtcNow;
        RevokedReason = reason;
        MarkAsUpdated();
    }

    public void RecordUsage()
    {
        LastUsedAt = DateTime.UtcNow;
        MarkAsUpdated();
    }
}
