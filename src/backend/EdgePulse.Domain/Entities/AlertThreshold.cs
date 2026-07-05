using EdgePulse.Domain.Common;

namespace EdgePulse.Domain.Entities;

/// <summary>
/// Defines the conditions under which an alert fires for a specific
/// device metric. Supports min/max range checks with consecutive-
/// breach counting to suppress transient noise.
/// </summary>
public class AlertThreshold : TenantBaseEntity
{
    public Guid DeviceId { get; private set; }

    /// <summary>Metric key matching TelemetryReading.Metrics[].Key</summary>
    public string MetricKey { get; private set; } = string.Empty;

    /// <summary>
    /// Display name shown in the UI.
    /// e.g. "Bearing Temperature High"
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Alert fires when value drops BELOW this. Null = no lower bound.</summary>
    public double? MinValue { get; private set; }

    /// <summary>Alert fires when value rises ABOVE this. Null = no upper bound.</summary>
    public double? MaxValue { get; private set; }

    /// <summary>Unit of measure for display (°C, bar, RPM, …)</summary>
    public string? Unit { get; private set; }

    /// <summary>
    /// Severity code — CRITICAL, HIGH, MEDIUM, LOW.
    /// Stored as a string so TelemetryProcessor can use it
    /// without joining to the AlertSeverities lookup table.
    /// </summary>
    public string SeverityCode { get; private set; } = "HIGH";

    /// <summary>
    /// Number of consecutive readings that must breach the threshold
    /// before an alert is fired. Default = 3. Prevents transient spikes
    /// from generating alert noise.
    /// </summary>
    public int ConsecutiveCount { get; private set; } = 3;

    public bool IsActive { get; private set; } = true;

    /// <summary>Optional description / remediation hint for operators.</summary>
    public string? Description { get; private set; }

    // Navigation
    public Device? Device { get; private set; }

    protected AlertThreshold() { }

    public static AlertThreshold Create(
        Guid tenantId,
        Guid deviceId,
        string metricKey,
        string name,
        double? minValue,
        double? maxValue,
        string severityCode = "HIGH",
        string? unit = null,
        int consecutiveCount = 3,
        string? description = null)
    {
        if (minValue is null && maxValue is null)
            throw new ArgumentException(
                "At least one of MinValue or MaxValue must be specified.");

        return new AlertThreshold
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DeviceId = deviceId,
            MetricKey = metricKey.ToLowerInvariant(),
            Name = name,
            MinValue = minValue,
            MaxValue = maxValue,
            SeverityCode = severityCode.ToUpperInvariant(),
            Unit = unit,
            ConsecutiveCount = consecutiveCount < 1 ? 1 : consecutiveCount,
            IsActive = true,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(
        string name,
        double? minValue,
        double? maxValue,
        string severityCode,
        string? unit,
        int consecutiveCount,
        string? description)
    {
        if (minValue is null && maxValue is null)
            throw new ArgumentException(
                "At least one of MinValue or MaxValue must be specified.");

        Name = name;
        MinValue = minValue;
        MaxValue = maxValue;
        SeverityCode = severityCode.ToUpperInvariant();
        Unit = unit;
        ConsecutiveCount = consecutiveCount < 1 ? 1 : consecutiveCount;
        Description = description;
        MarkAsUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }
}
