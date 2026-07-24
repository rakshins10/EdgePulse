using EdgePulse.Domain.Common;

namespace EdgePulse.Domain.Entities;

/// <summary>
/// A maintenance work order. Created manually or auto-created by the alert
/// engine (closing the loop alert → action → resolution).
///
/// Status lifecycle:
///   OPEN → INPROGRESS → COMPLETED
///   OPEN/INPROGRESS/ONHOLD → CANCELLED
///   INPROGRESS ↔ ONHOLD
/// COMPLETED and CANCELLED are terminal.
/// </summary>
public class WorkOrder : TenantBaseEntity
{
    public const string StatusOpen = "OPEN";
    public const string StatusInProgress = "INPROGRESS";
    public const string StatusOnHold = "ONHOLD";
    public const string StatusCompleted = "COMPLETED";
    public const string StatusCancelled = "CANCELLED";

    public string Number { get; private set; } = string.Empty;   // e.g. WO-3F2A9C41
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid DeviceId { get; private set; }
    public Guid MillId { get; private set; }
    public Guid? AlertId { get; private set; }                   // set when auto-created
    public Guid? MaintenanceTypeId { get; private set; }
    public string Priority { get; private set; } = "MEDIUM";     // LOW/MEDIUM/HIGH/CRITICAL
    public string Status { get; private set; } = StatusOpen;
    public string? AssignedTo { get; private set; }              // username/email
    public DateTime? DueDate { get; private set; }
    public string? PartsUsed { get; private set; }               // free-text parts/materials
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTime? CompletedAt { get; private set; }
    public string? CompletedBy { get; private set; }
    public string? CompletionNotes { get; private set; }

    protected WorkOrder() { }

    public static WorkOrder Create(
        Guid tenantId,
        Guid deviceId,
        Guid millId,
        string title,
        string createdBy,
        string priority = "MEDIUM",
        string? description = null,
        Guid? alertId = null,
        Guid? maintenanceTypeId = null,
        DateTime? dueDate = null)
    {
        var id = Guid.NewGuid();
        return new WorkOrder
        {
            Id = id,
            TenantId = tenantId,
            DeviceId = deviceId,
            MillId = millId,
            Number = $"WO-{id.ToString("N")[..8].ToUpperInvariant()}",
            Title = title,
            Description = description,
            AlertId = alertId,
            MaintenanceTypeId = maintenanceTypeId,
            Priority = priority,
            Status = StatusOpen,
            DueDate = dueDate,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Assign(string? assignee)
    {
        EnsureNotTerminal();
        AssignedTo = string.IsNullOrWhiteSpace(assignee) ? null : assignee.Trim();
        MarkAsUpdated();
    }

    public void Start()
    {
        EnsureTransition(StatusInProgress, StatusOpen, StatusOnHold);
        Status = StatusInProgress;
        MarkAsUpdated();
    }

    public void Hold()
    {
        EnsureTransition(StatusOnHold, StatusInProgress);
        Status = StatusOnHold;
        MarkAsUpdated();
    }

    public void Complete(string completedBy, string? notes, string? partsUsed)
    {
        EnsureTransition(StatusCompleted, StatusInProgress);
        Status = StatusCompleted;
        CompletedAt = DateTime.UtcNow;
        CompletedBy = completedBy;
        CompletionNotes = notes;
        if (!string.IsNullOrWhiteSpace(partsUsed)) PartsUsed = partsUsed;
        MarkAsUpdated();
    }

    public void Cancel()
    {
        EnsureTransition(StatusCancelled, StatusOpen, StatusInProgress, StatusOnHold);
        Status = StatusCancelled;
        MarkAsUpdated();
    }

    public void UpdateDetails(
        string title, string? description, string priority, DateTime? dueDate,
        Guid? maintenanceTypeId, string? partsUsed)
    {
        EnsureNotTerminal();
        Title = title;
        Description = description;
        Priority = priority;
        DueDate = dueDate;
        MaintenanceTypeId = maintenanceTypeId;
        PartsUsed = partsUsed;
        MarkAsUpdated();
    }

    private void EnsureNotTerminal()
    {
        if (Status is StatusCompleted or StatusCancelled)
            throw new InvalidOperationException(
                $"Work order {Number} is {Status} and can no longer be modified.");
    }

    private void EnsureTransition(string target, params string[] allowedFrom)
    {
        if (!allowedFrom.Contains(Status))
            throw new InvalidOperationException(
                $"Cannot move work order {Number} from {Status} to {target}.");
    }
}
