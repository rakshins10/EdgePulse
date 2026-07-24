using EdgePulse.Domain.Common;

namespace EdgePulse.Domain.Entities;

/// <summary>
/// An in-app notification for users of a tenant. Created by the system
/// (e.g. the alert engine when a threshold fires) and shown in the
/// dashboard's notification center until marked read.
///
/// Type is an extensible discriminator ("ALERT" today; "WORKORDER",
/// "SYSTEM", … later). LinkEntityType/LinkEntityId let the UI deep-link
/// to the related record.
/// </summary>
public class Notification : BaseEntity
{
    public Guid TenantId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string? SeverityCode { get; private set; }   // e.g. CRITICAL/HIGH/MEDIUM/LOW
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string? LinkEntityType { get; private set; } // e.g. "Alert", "Device"
    public Guid? LinkEntityId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }

    protected Notification() { }

    public static Notification Create(
        Guid tenantId,
        string type,
        string title,
        string message,
        string? severityCode = null,
        string? linkEntityType = null,
        Guid? linkEntityId = null)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Type = type,
            Title = title,
            Message = message,
            SeverityCode = severityCode,
            LinkEntityType = linkEntityType,
            LinkEntityId = linkEntityId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void MarkRead()
    {
        if (IsRead) return;
        IsRead = true;
        ReadAt = DateTime.UtcNow;
        MarkAsUpdated();
    }
}
