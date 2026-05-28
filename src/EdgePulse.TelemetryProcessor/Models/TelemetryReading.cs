namespace EdgePulse.TelemetryProcessor.Models;

/// <summary>
/// Incoming message deserialized from the RabbitMQ telemetry.readings queue.
/// Published by the device simulator (EdgePulse.Ingestion).
/// </summary>
public class TelemetryReading
{
    public Guid DeviceId { get; set; }
    public Guid TenantId { get; set; }
    public Guid MillId { get; set; }
    public Guid AreaId { get; set; }
    public DateTime Timestamp { get; set; }
    public List<MetricReading> Metrics { get; set; } = [];
    public DateTime ReceivedAt { get; set; }
}

public class MetricReading
{
    public string Key { get; set; } = string.Empty;
    public double Value { get; set; }
    public string? Unit { get; set; }
}
