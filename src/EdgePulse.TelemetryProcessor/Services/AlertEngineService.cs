using EdgePulse.TelemetryProcessor.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace EdgePulse.TelemetryProcessor.Services;

/// <summary>
/// Core alert engine. For each incoming telemetry reading:
///   1. Check each metric against active thresholds (via ThresholdCacheService)
///   2. Track consecutive breach count in memory per (deviceId, metricKey, thresholdId)
///   3. When count >= ConsecutiveCount, fire an alert to SQL Server
///   4. Reset counter when the value returns to normal
///
/// Alert deduplication: a threshold only fires ONE alert at a time.
/// A new alert is not created if an OPEN alert already exists for
/// the same (deviceId, metricKey, thresholdId).
/// </summary>
public class AlertEngineService
{
    private readonly ThresholdCacheService _thresholds;
    private readonly string _sqlConnectionString;
    private readonly ILogger<AlertEngineService> _logger;

    // Key: "{deviceId}:{metricKey}:{thresholdId}"
    // Value: consecutive breach count
    private readonly ConcurrentDictionary<string, int> _breachCounters = new();

    // Tracks which (deviceId, metricKey, thresholdId) currently have an OPEN alert
    // to avoid creating duplicates. Refreshed after each successful alert write.
    private readonly ConcurrentDictionary<string, bool> _openAlertKeys = new();

    public AlertEngineService(
        ThresholdCacheService thresholds,
        string sqlConnectionString,
        ILogger<AlertEngineService> logger)
    {
        _thresholds = thresholds;
        _sqlConnectionString = sqlConnectionString;
        _logger = logger;
    }

    /// <summary>
    /// Evaluate a telemetry reading against all active thresholds.
    /// Called once per reading after MongoDB store.
    /// </summary>
    public async Task EvaluateAsync(
        TelemetryReading reading,
        CancellationToken ct = default)
    {
        foreach (var metric in reading.Metrics)
        {
            var applicableThresholds = await _thresholds.GetThresholdsAsync(
                reading.DeviceId, metric.Key, ct);

            foreach (var threshold in applicableThresholds)
            {
                await EvaluateMetricAsync(reading, metric, threshold, ct);
            }
        }
    }

    private async Task EvaluateMetricAsync(
        TelemetryReading reading,
        MetricReading metric,
        ThresholdCacheEntry threshold,
        CancellationToken ct)
    {
        var counterKey =
            $"{reading.DeviceId}:{metric.Key.ToLowerInvariant()}:{threshold.Id}";
        var openAlertKey = counterKey;

        if (threshold.IsBreached(metric.Value))
        {
            var count = _breachCounters.AddOrUpdate(
                counterKey,
                1,
                (_, current) => current + 1);

            _logger.LogDebug(
                "Breach {Count}/{Required} — Device {DeviceId} Metric {Metric} " +
                "Value {Value} Threshold {ThresholdId}",
                count, threshold.ConsecutiveCount,
                reading.DeviceId, metric.Key, metric.Value, threshold.Id);

            if (count >= threshold.ConsecutiveCount)
            {
                // Only fire if no open alert exists for this threshold
                if (!_openAlertKeys.ContainsKey(openAlertKey))
                {
                    var hasOpen = await HasOpenAlertAsync(
                        reading.DeviceId, metric.Key, threshold.Id, ct);

                    if (!hasOpen)
                    {
                        await CreateAlertAsync(reading, metric, threshold, ct);
                        _openAlertKeys[openAlertKey] = true;

                        // Reset counter after firing
                        _breachCounters[counterKey] = 0;
                    }
                }
            }
        }
        else
        {
            // Value is back to normal — reset counter and clear open alert flag
            if (_breachCounters.TryRemove(counterKey, out var prevCount) && prevCount > 0)
            {
                _logger.LogDebug(
                    "Threshold cleared — Device {DeviceId} Metric {Metric} " +
                    "Value {Value} (was {Count} consecutive breaches)",
                    reading.DeviceId, metric.Key, metric.Value, prevCount);
            }

            // Remove open alert flag so the next breach cycle can fire a new alert
            _openAlertKeys.TryRemove(openAlertKey, out _);
        }
    }

    private async Task<bool> HasOpenAlertAsync(
        Guid deviceId, string metricKey, Guid thresholdId,
        CancellationToken ct)
    {
        try
        {
            await using var conn = new SqlConnection(_sqlConnectionString);
            await conn.OpenAsync(ct);

            const string sql = """
                SELECT COUNT(1)
                FROM   Alerts
                WHERE  DeviceId = @DeviceId
                  AND  MetricKey = @MetricKey
                  AND  AlertThresholdId = @ThresholdId
                  AND  StatusCode IN ('OPEN', 'ACKNOWLEDGED')
                """;

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@DeviceId", deviceId);
            cmd.Parameters.AddWithValue("@MetricKey", metricKey.ToLowerInvariant());
            cmd.Parameters.AddWithValue("@ThresholdId", thresholdId);

            var count = (int)(await cmd.ExecuteScalarAsync(ct) ?? 0);
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error checking existing open alert for Device {DeviceId} " +
                "Metric {Metric} Threshold {ThresholdId}",
                deviceId, metricKey, thresholdId);
            // Safe default: assume open alert exists to avoid alert spam
            return true;
        }
    }

    private async Task CreateAlertAsync(
        TelemetryReading reading,
        MetricReading metric,
        ThresholdCacheEntry threshold,
        CancellationToken ct)
    {
        try
        {
            // Capture the reading for context (stored as JSON)
            var readingSnapshot = new[]
            {
                new { reading.Timestamp, metric.Value }
            };
            var readingsJson = JsonSerializer.Serialize(readingSnapshot);

            var alertId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            await using var conn = new SqlConnection(_sqlConnectionString);
            await conn.OpenAsync(ct);

            const string sql = """
                INSERT INTO Alerts (
                    Id, TenantId, DeviceId, MillId, AreaId,
                    AlertThresholdId, MetricKey,
                    TriggerValue, ThresholdValue, Unit,
                    SeverityCode, StatusCode,
                    ReadingsJson, AiSummary,
                    TriggeredAt, AcknowledgedAt, AcknowledgedBy,
                    ResolvedAt, ResolvedBy, Notes,
                    CreatedAt, UpdatedAt,
                    IsDeleted, DeletedAt
                ) VALUES (
                    @Id, @TenantId, @DeviceId, @MillId, @AreaId,
                    @ThresholdId, @MetricKey,
                    @TriggerValue, @ThresholdValue, @Unit,
                    @SeverityCode, 'OPEN',
                    @ReadingsJson, NULL,
                    @TriggeredAt, NULL, NULL,
                    NULL, NULL, NULL,
                    @CreatedAt, @UpdatedAt,
                    0, NULL
                )
                """;

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", alertId);
            cmd.Parameters.AddWithValue("@TenantId", threshold.TenantId);
            cmd.Parameters.AddWithValue("@DeviceId", reading.DeviceId);
            cmd.Parameters.AddWithValue("@MillId", reading.MillId);
            cmd.Parameters.AddWithValue("@AreaId", reading.AreaId);
            cmd.Parameters.AddWithValue("@ThresholdId", threshold.Id);
            cmd.Parameters.AddWithValue("@MetricKey", metric.Key.ToLowerInvariant());
            cmd.Parameters.AddWithValue("@TriggerValue", metric.Value);
            cmd.Parameters.AddWithValue("@ThresholdValue",
                threshold.GetBreachedThresholdValue(metric.Value));
            cmd.Parameters.AddWithValue("@Unit",
                (object?)metric.Unit ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SeverityCode", threshold.SeverityCode);
            cmd.Parameters.AddWithValue("@ReadingsJson", readingsJson);
            cmd.Parameters.AddWithValue("@TriggeredAt", now);
            cmd.Parameters.AddWithValue("@CreatedAt", now);
            cmd.Parameters.AddWithValue("@UpdatedAt", now);

            await cmd.ExecuteNonQueryAsync(ct);

            _logger.LogWarning(
                "ALERT FIRED [{Severity}] — Device {DeviceId} Metric {Metric} " +
                "Value {Value} (Threshold: {ThresholdValue}) AlertId: {AlertId}",
                threshold.SeverityCode,
                reading.DeviceId, metric.Key, metric.Value,
                threshold.GetBreachedThresholdValue(metric.Value),
                alertId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to create alert for Device {DeviceId} Metric {Metric} " +
                "Threshold {ThresholdId}",
                reading.DeviceId, metric.Key, threshold.Id);
        }
    }
}
