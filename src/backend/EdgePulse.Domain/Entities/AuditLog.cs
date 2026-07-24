using EdgePulse.Domain.Common;

namespace EdgePulse.Domain.Entities;

/// <summary>
/// One row per entity write (create / update / delete), captured automatically
/// by the DbContext on SaveChanges. ChangesJson holds property-level
/// old → new values for updates. Immutable once written.
/// </summary>
public class AuditLog
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string UserName { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;      // CREATED / UPDATED / DELETED
    public string EntityType { get; private set; } = string.Empty;  // e.g. "Device"
    public Guid EntityId { get; private set; }
    public string? EntityDisplay { get; private set; }              // e.g. device name
    public string? ChangesJson { get; private set; }                // {"Prop":{"old":..,"new":..}}
    public DateTime Timestamp { get; private set; }

    protected AuditLog() { }

    public static AuditLog Create(
        Guid tenantId,
        string userName,
        string action,
        string entityType,
        Guid entityId,
        string? entityDisplay,
        string? changesJson)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserName = userName,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            EntityDisplay = entityDisplay,
            ChangesJson = changesJson,
            Timestamp = DateTime.UtcNow
        };
    }
}
