using EdgePulse.Domain.Common;

namespace EdgePulse.Domain.Entities;

/// <summary>
/// An alert instance fired when a device metric crosses a threshold.
/// Follows the state machine:
///   Open → Acknowledged → Resolved → Closed
///   Open → Resolved (direct close by admin)
/// Resolved and Closed are terminal states.
/// </summary>
public class Alert : TenantBaseEntity
{
    public Guid DeviceId { get; private set; }
    public Guid MillId { get; private set; }
    public Guid AreaId { get; private set; }
    public Guid AlertThresholdId { get; private set; }

    /// <summary>Metric key that triggered the alert (e.g. "temperature")</summary>
    public string MetricKey { get; private set; } = string.Empty;

    /// <summary>The reading value that finally triggered the alert</summary>
    public double TriggerValue { get; private set; }

    /// <summary>The threshold value that was crossed (MinValue or MaxValue)</summary>
    public double ThresholdValue { get; private set; }

    /// <summary>Unit of measure (°C, bar, RPM, …)</summary>
    public string? Unit { get; private set; }

    /// <summary>Severity code: CRITICAL, HIGH, MEDIUM, LOW</summary>
    public string SeverityCode { get; private set; } = "HIGH";

    /// <summary>
    /// Current alert status code: OPEN, ACKNOWLEDGED, RESOLVED, CLOSED.
    /// Mirrors GenericAlertStatusIds codes.
    /// </summary>
    public string StatusCode { get; private set; } = "OPEN";

    /// <summary>
    /// JSON array of the N consecutive readings that triggered the alert.
    /// Stored as string to avoid extra join. Format:
    /// [{"timestamp":"...","value":42.5},...]
    /// </summary>
    public string? ReadingsJson { get; private set; }

    /// <summary>
    /// AI-generated summary for operators. Populated asynchronously
    /// after alert creation (Sprint 9+).
    /// </summary>
    public string? AiSummary { get; private set; }

    public DateTime TriggeredAt { get; private set; }

    public DateTime? AcknowledgedAt { get; private set; }
    public string? AcknowledgedBy { get; private set; }

    public DateTime? ResolvedAt { get; private set; }
    public string? ResolvedBy { get; private set; }

    /// <summary>Operator notes added when acknowledging or resolving</summary>
    public string? Notes { get; private set; }

    // Navigations
    public Device? Device { get; private set; }
    public AlertThreshold? Threshold { get; private set; }

    protected Alert() { }

    /// <summary>
    /// Called by the TelemetryProcessor alert engine when consecutive
    /// breach count is reached.
    /// </summary>
    public static Alert Create(
        Guid tenantId,
        Guid deviceId,
        Guid millId,
        Guid areaId,
        Guid alertThresholdId,
        string metricKey,
        double triggerValue,
        double thresholdValue,
        string severityCode,
        string? unit = null,
        string? readingsJson = null)
    {
        return new Alert
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DeviceId = deviceId,
            MillId = millId,
            AreaId = areaId,
            AlertThresholdId = alertThresholdId,
            MetricKey = metricKey.ToLowerInvariant(),
            TriggerValue = triggerValue,
            ThresholdValue = thresholdValue,
            SeverityCode = severityCode.ToUpperInvariant(),
            StatusCode = "OPEN",
            Unit = unit,
            ReadingsJson = readingsJson,
            TriggeredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Operator acknowledges the alert — sets status to ACKNOWLEDGED.</summary>
    public void Acknowledge(string acknowledgedBy, string? notes = null)
    {
        if (StatusCode == "RESOLVED" || StatusCode == "CLOSED")
            throw new InvalidOperationException(
                $"Cannot acknowledge alert in terminal state '{StatusCode}'.");

        StatusCode = "ACKNOWLEDGED";
        AcknowledgedAt = DateTime.UtcNow;
        AcknowledgedBy = acknowledgedBy;
        if (notes is not null) Notes = notes;
        MarkAsUpdated();
    }

    /// <summary>Operator marks the issue as resolved.</summary>
    public void Resolve(string resolvedBy, string? notes = null)
    {
        if (StatusCode == "CLOSED")
            throw new InvalidOperationException(
                "Cannot resolve alert in terminal state 'CLOSED'.");

        if (StatusCode == "OPEN")
        {
            // Auto-acknowledge when resolving directly from OPEN
            AcknowledgedAt = DateTime.UtcNow;
            AcknowledgedBy = resolvedBy;
        }

        StatusCode = "RESOLVED";
        ResolvedAt = DateTime.UtcNow;
        ResolvedBy = resolvedBy;
        if (notes is not null) Notes = notes;
        MarkAsUpdated();
    }

    /// <summary>
    /// CustomerAdmin or MillManager closes a resolved alert.
    /// Terminal state — no further transitions.
    /// </summary>
    public void Close()
    {
        if (StatusCode != "RESOLVED")
            throw new InvalidOperationException(
                $"Only RESOLVED alerts can be closed. Current status: '{StatusCode}'.");

        StatusCode = "CLOSED";
        MarkAsUpdated();
    }

    /// <summary>Set AI summary after async generation.</summary>
    public void SetAiSummary(string summary)
    {
        AiSummary = summary;
        MarkAsUpdated();
    }
}
