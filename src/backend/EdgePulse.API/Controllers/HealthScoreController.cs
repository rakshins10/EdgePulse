using System.Security.Claims;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Application.Features.Health;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EdgePulse.API.Controllers;

/// <summary>
/// Statistical device-health scoring (transparent arithmetic, not ML):
/// combines Mongo telemetry trends (7-day daily averages per metric) with
/// SQL alert thresholds and open alerts into a 0–100 score, a grade and a
/// naive linear days-to-threshold estimate. Route: /api/healthscore
/// (NB: /health is the liveness probe).
/// </summary>
[ApiController]
[Authorize]
[Route("api/healthscore")]
public class HealthScoreController : ControllerBase
{
    private readonly IMongoCollection<TelemetryReadingDocument> _readings;
    private readonly IApplicationDbContext _db;

    public HealthScoreController(IConfiguration configuration, IApplicationDbContext db)
    {
        var client = new MongoClient(configuration.GetConnectionString("MongoDB"));
        var database = configuration["MongoDB:Database"] ?? "edgepulse_telemetry";
        _readings = client.GetDatabase(database)
            .GetCollection<TelemetryReadingDocument>("telemetry_readings");
        _db = db;
    }

    /// <summary>
    /// Health scores for every device with telemetry in the last 7 days.
    /// </summary>
    [HttpGet("devices")]
    [ProducesResponseType(typeof(List<DeviceHealthDto>), 200)]
    public async Task<IActionResult> GetDeviceHealth(
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(User.FindFirstValue("tenantId"), out var tenantId))
            return Unauthorized();

        var since = DateTime.UtcNow.AddDays(-7);

        // Daily average per device+metric (7-day window), computed in Mongo
        var pipeline = new[]
        {
            new BsonDocument("$match", new BsonDocument
            {
                ["TenantId"] = tenantId.ToString(),
                ["Timestamp"] = new BsonDocument("$gte", since),
            }),
            new BsonDocument("$unwind", "$Metrics"),
            new BsonDocument("$group", new BsonDocument
            {
                ["_id"] = new BsonDocument
                {
                    ["device"] = "$DeviceId",
                    ["metric"] = "$Metrics.Key",
                    ["day"] = new BsonDocument("$dateToString", new BsonDocument
                    {
                        ["format"] = "%Y-%m-%d",
                        ["date"] = "$Timestamp",
                    }),
                },
                ["avg"] = new BsonDocument("$avg", "$Metrics.Value"),
            }),
        };

        var buckets = await _readings
            .Aggregate<BsonDocument>(pipeline, cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken);

        var series = buckets
            .Select(b => new
            {
                DeviceId = Guid.Parse(b["_id"]["device"].AsString),
                Metric = b["_id"]["metric"].AsString,
                Day = b["_id"]["day"].AsString,
                Avg = b["avg"].ToDouble(),
            })
            .GroupBy(x => (x.DeviceId, x.Metric))
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(x => x.Day).Select(x => x.Avg).ToList());

        var deviceIds = series.Keys.Select(k => k.DeviceId).Distinct().ToList();

        var devices = await _db.Devices
            .Where(d => d.TenantId == tenantId && deviceIds.Contains(d.Id) && !d.IsDeleted)
            .Select(d => new { d.Id, d.Name, d.Code, d.MillId })
            .ToListAsync(cancellationToken);

        var millNames = await _db.Mills
            .Where(m => m.TenantId == tenantId)
            .ToDictionaryAsync(m => m.Id, m => m.Name, cancellationToken);

        var thresholds = await _db.AlertThresholds
            .Where(t => t.TenantId == tenantId && t.IsActive)
            .Select(t => new { t.DeviceId, t.MetricKey, t.MinValue, t.MaxValue })
            .ToListAsync(cancellationToken);

        var openAlerts = await _db.Alerts
            .Where(a => a.TenantId == tenantId && !a.IsDeleted &&
                        (a.StatusCode == "OPEN" || a.StatusCode == "ACKNOWLEDGED"))
            .Select(a => new { a.DeviceId, a.SeverityCode })
            .ToListAsync(cancellationToken);

        var result = new List<DeviceHealthDto>();
        foreach (var device in devices)
        {
            var alerts = openAlerts.Where(a => a.DeviceId == device.Id).ToList();
            var alertPenalty = HealthMath.AlertPenalty(
                alerts.Count(a => a.SeverityCode == "CRITICAL"),
                alerts.Count(a => a.SeverityCode == "HIGH"),
                alerts.Count(a => a.SeverityCode == "MEDIUM"),
                alerts.Count(a => a.SeverityCode == "LOW"));

            // Evaluate every thresholded metric; keep the worst one
            MetricHealthDto? worst = null;
            var worstPenalty = 0;
            foreach (var threshold in thresholds.Where(t => t.DeviceId == device.Id))
            {
                if (!series.TryGetValue(
                        (device.Id, threshold.MetricKey.ToLowerInvariant()), out var daily))
                    continue;

                var average = daily[^1]; // most recent day's average
                var utilization = HealthMath.UtilizationPercent(
                    average, threshold.MinValue, threshold.MaxValue);
                var slope = HealthMath.SlopePerDay(daily);
                var daysOut = HealthMath.DaysToThreshold(average, threshold.MaxValue, slope);

                var penalty = HealthMath.UtilizationPenalty(utilization)
                            + HealthMath.TrendPenalty(daysOut);
                if (penalty >= worstPenalty)
                {
                    worstPenalty = penalty;
                    worst = new MetricHealthDto(
                        threshold.MetricKey, Math.Round(average, 2),
                        threshold.MaxValue, utilization, slope, daysOut);
                }
            }

            var score = HealthMath.Score(
                alertPenalty,
                worst is null ? 0 : HealthMath.UtilizationPenalty(worst.UtilizationPercent),
                worst is null ? 0 : HealthMath.TrendPenalty(worst.DaysToThreshold));

            result.Add(new DeviceHealthDto(
                device.Id, device.Name, device.Code,
                millNames.GetValueOrDefault(device.MillId, ""),
                score, HealthMath.Grade(score),
                alerts.Count, worst));
        }

        return Ok(result.OrderBy(r => r.Score).ToList());
    }
}

public record DeviceHealthDto(
    Guid DeviceId,
    string DeviceName,
    string DeviceCode,
    string MillName,
    int Score,
    string Grade,
    int OpenAlerts,
    MetricHealthDto? WorstMetric
);

public record MetricHealthDto(
    string MetricKey,
    double RecentAverage,
    double? ThresholdMax,
    double UtilizationPercent,
    double TrendPerDay,
    double? DaysToThreshold
);
