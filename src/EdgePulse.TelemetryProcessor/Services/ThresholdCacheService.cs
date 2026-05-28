using EdgePulse.TelemetryProcessor.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace EdgePulse.TelemetryProcessor.Services;

/// <summary>
/// Loads all active AlertThresholds from SQL Server into a ConcurrentDictionary
/// keyed by (DeviceId, MetricKey). Refreshes on a configurable interval so the
/// processor picks up newly configured thresholds without restart.
/// </summary>
public class ThresholdCacheService
{
    private readonly string _connectionString;
    private readonly ILogger<ThresholdCacheService> _logger;
    private readonly TimeSpan _refreshInterval;

    // Key: "{deviceId}:{metricKey}" → list of thresholds for that device+metric
    private volatile ConcurrentDictionary<string, List<ThresholdCacheEntry>>
        _cache = new();

    private DateTime _lastRefreshAt = DateTime.MinValue;

    public ThresholdCacheService(
        string connectionString,
        int refreshSeconds,
        ILogger<ThresholdCacheService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
        _refreshInterval = TimeSpan.FromSeconds(refreshSeconds);
    }

    /// <summary>
    /// Returns thresholds for (deviceId, metricKey).
    /// Refreshes the cache if the interval has elapsed.
    /// </summary>
    public async Task<IReadOnlyList<ThresholdCacheEntry>> GetThresholdsAsync(
        Guid deviceId,
        string metricKey,
        CancellationToken ct = default)
    {
        if (DateTime.UtcNow - _lastRefreshAt > _refreshInterval)
            await RefreshAsync(ct);

        var key = $"{deviceId}:{metricKey.ToLowerInvariant()}";
        return _cache.TryGetValue(key, out var list)
            ? list.AsReadOnly()
            : Array.Empty<ThresholdCacheEntry>();
    }

    /// <summary>Force reload all active thresholds from the database.</summary>
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        try
        {
            var newCache = new ConcurrentDictionary<string, List<ThresholdCacheEntry>>();

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            const string sql = """
                SELECT Id, TenantId, DeviceId, MetricKey,
                       MinValue, MaxValue, SeverityCode, ConsecutiveCount
                FROM   AlertThresholds
                WHERE  IsActive = 1
                  AND  IsDeleted = 0
                """;

            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                var entry = new ThresholdCacheEntry(
                    Id: reader.GetGuid(0),
                    TenantId: reader.GetGuid(1),
                    DeviceId: reader.GetGuid(2),
                    MetricKey: reader.GetString(3),
                    MinValue: reader.IsDBNull(4) ? null : reader.GetDouble(4),
                    MaxValue: reader.IsDBNull(5) ? null : reader.GetDouble(5),
                    SeverityCode: reader.GetString(6),
                    ConsecutiveCount: reader.GetInt32(7));

                var cacheKey = $"{entry.DeviceId}:{entry.MetricKey.ToLowerInvariant()}";
                newCache.AddOrUpdate(
                    cacheKey,
                    _ => [entry],
                    (_, existing) => { existing.Add(entry); return existing; });
            }

            _cache = newCache;
            _lastRefreshAt = DateTime.UtcNow;

            var totalThresholds = newCache.Values.Sum(x => x.Count);
            _logger.LogDebug(
                "Threshold cache refreshed. {Count} active thresholds across {Keys} device-metric pairs.",
                totalThresholds, newCache.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to refresh threshold cache. " +
                "Using stale cache until next interval.");
        }
    }
}

public record ThresholdCacheEntry(
    Guid Id,
    Guid TenantId,
    Guid DeviceId,
    string MetricKey,
    double? MinValue,
    double? MaxValue,
    string SeverityCode,
    int ConsecutiveCount
)
{
    /// <summary>Returns true if the value breaches this threshold.</summary>
    public bool IsBreached(double value)
    {
        if (MinValue.HasValue && value < MinValue.Value) return true;
        if (MaxValue.HasValue && value > MaxValue.Value) return true;
        return false;
    }

    /// <summary>The threshold value that was crossed (for alert record).</summary>
    public double GetBreachedThresholdValue(double value)
    {
        if (MinValue.HasValue && value < MinValue.Value) return MinValue.Value;
        if (MaxValue.HasValue && value > MaxValue.Value) return MaxValue.Value;
        return 0;
    }
}
