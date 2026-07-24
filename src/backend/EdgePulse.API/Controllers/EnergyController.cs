using System.Security.Claims;
using System.Text;
using EdgePulse.Application.Common;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Application.Features.Energy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EdgePulse.API.Controllers;

/// <summary>
/// Energy monitoring & ESG reporting. Aggregates instantaneous power metrics
/// (kW) from MongoDB telemetry into daily energy (kWh) and CO₂-equivalent
/// figures. Follows the TelemetryController precedent of querying Mongo
/// directly (time-series data never touches EF/SQL).
///
/// Config:
///   Esg:PowerMetricKeys     — metric keys treated as instantaneous kW
///                             (default ["power_consumption"])
///   Esg:Co2FactorKgPerKwh   — grid carbon intensity (default 0.181 kg/kWh,
///                             ~EU-27 average; set per deployment/country)
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class EnergyController : ControllerBase
{
    private readonly IMongoCollection<TelemetryReadingDocument> _readings;
    private readonly IApplicationDbContext _db;
    private readonly string[] _powerKeys;
    private readonly double _co2Factor;

    public EnergyController(IConfiguration configuration, IApplicationDbContext db)
    {
        var client = new MongoClient(configuration.GetConnectionString("MongoDB"));
        var database = configuration["MongoDB:Database"] ?? "edgepulse_telemetry";
        _readings = client.GetDatabase(database)
            .GetCollection<TelemetryReadingDocument>("telemetry_readings");
        _db = db;
        _powerKeys = configuration.GetSection("Esg:PowerMetricKeys").Get<string[]>()
            ?? ["power_consumption"];
        _co2Factor = configuration.GetValue("Esg:Co2FactorKgPerKwh", 0.181);
    }

    /// <summary>
    /// ESG energy report for a date range (default: last 30 days) —
    /// totals, per-device and per-mill energy + CO₂, and a daily series.
    /// </summary>
    [HttpGet("report")]
    [ProducesResponseType(typeof(EnergyReport), 200)]
    public async Task<IActionResult> GetReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var report = await BuildReportAsync(from, to, cancellationToken);
        return report is null ? Unauthorized() : Ok(report);
    }

    /// <summary>
    /// The ESG report as CSV (per-device rows).
    /// </summary>
    [HttpGet("report/csv")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetReportCsv(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var report = await BuildReportAsync(from, to, cancellationToken);
        if (report is null) return Unauthorized();

        var csv = CsvBuilder.Build(
            ["Device", "Code", "Mill", "Avg power (kW)", "Energy (kWh)", "CO2 (kg)"],
            report.Devices.Select(d => (IEnumerable<object?>)
                [d.DeviceName, d.DeviceCode, d.MillName,
                 d.AvgPowerKw, d.EnergyKwh, d.Co2Kg]));

        return File(
            Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray(),
            "text/csv; charset=utf-8",
            $"esg-energy_{report.From:yyyyMMdd}-{report.To:yyyyMMdd}.csv");
    }

    private async Task<EnergyReport?> BuildReportAsync(
        DateTime? from, DateTime? to, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue("tenantId"), out var tenantId))
            return null;

        var end = to?.ToUniversalTime() ?? DateTime.UtcNow;
        var start = from?.ToUniversalTime() ?? end.AddDays(-30);

        // Daily buckets per device, aggregated inside Mongo so only the small
        // grouped result crosses the wire.
        var pipeline = new[]
        {
            new BsonDocument("$match", new BsonDocument
            {
                // Guids are serialized as strings (global GuidSerializer registration)
                ["TenantId"] = tenantId.ToString(),
                ["Timestamp"] = new BsonDocument { ["$gte"] = start, ["$lte"] = end },
                ["Metrics.Key"] = new BsonDocument("$in", new BsonArray(_powerKeys)),
            }),
            new BsonDocument("$unwind", "$Metrics"),
            new BsonDocument("$match", new BsonDocument(
                "Metrics.Key", new BsonDocument("$in", new BsonArray(_powerKeys)))),
            new BsonDocument("$group", new BsonDocument
            {
                ["_id"] = new BsonDocument
                {
                    ["device"] = "$DeviceId",
                    ["mill"] = "$MillId",
                    ["day"] = new BsonDocument("$dateToString", new BsonDocument
                    {
                        ["format"] = "%Y-%m-%d",
                        ["date"] = "$Timestamp",
                    }),
                },
                ["avgPower"] = new BsonDocument("$avg", "$Metrics.Value"),
                ["firstTs"] = new BsonDocument("$min", "$Timestamp"),
                ["lastTs"] = new BsonDocument("$max", "$Timestamp"),
                ["samples"] = new BsonDocument("$sum", 1),
            }),
        };

        var buckets = await _readings
            .Aggregate<BsonDocument>(pipeline, cancellationToken: ct)
            .ToListAsync(ct);

        // Device / mill names from SQL
        var deviceIds = buckets
            .Select(b => Guid.Parse(b["_id"]["device"].AsString))
            .Distinct().ToList();
        var deviceNames = await _db.Devices
            .Where(d => deviceIds.Contains(d.Id))
            .Select(d => new { d.Id, d.Name, d.Code, d.MillId })
            .ToListAsync(ct);
        var millNames = await _db.Mills
            .Where(m => m.TenantId == tenantId)
            .Select(m => new { m.Id, m.Name })
            .ToDictionaryAsync(m => m.Id, m => m.Name, ct);

        var rows = buckets.Select(b => new
        {
            DeviceId = Guid.Parse(b["_id"]["device"].AsString),
            MillId = Guid.Parse(b["_id"]["mill"].AsString),
            Day = b["_id"]["day"].AsString,
            AvgPower = b["avgPower"].ToDouble(),
            EnergyKwh = EnergyMath.EnergyKwh(
                b["avgPower"].ToDouble(),
                b["firstTs"].ToUniversalTime(),
                b["lastTs"].ToUniversalTime()),
        }).ToList();

        var devices = rows
            .GroupBy(r => r.DeviceId)
            .Select(g =>
            {
                var info = deviceNames.FirstOrDefault(d => d.Id == g.Key);
                var kwh = Math.Round(g.Sum(r => r.EnergyKwh), 1);
                return new EnergyDeviceRow(
                    g.Key,
                    info?.Name ?? g.Key.ToString(),
                    info?.Code ?? "",
                    info is not null && millNames.TryGetValue(info.MillId, out var mn) ? mn : "",
                    Math.Round(g.Average(r => r.AvgPower), 1),
                    kwh,
                    EnergyMath.Co2Kg(kwh, _co2Factor));
            })
            .OrderByDescending(d => d.EnergyKwh)
            .ToList();

        var mills = rows
            .GroupBy(r => r.MillId)
            .Select(g =>
            {
                var kwh = Math.Round(g.Sum(r => r.EnergyKwh), 1);
                return new EnergyMillRow(
                    g.Key,
                    millNames.TryGetValue(g.Key, out var name) ? name : g.Key.ToString(),
                    kwh,
                    EnergyMath.Co2Kg(kwh, _co2Factor));
            })
            .OrderByDescending(m => m.EnergyKwh)
            .ToList();

        var daily = rows
            .GroupBy(r => r.Day)
            .Select(g =>
            {
                var kwh = Math.Round(g.Sum(r => r.EnergyKwh), 1);
                return new EnergyDailyPoint(g.Key, kwh, EnergyMath.Co2Kg(kwh, _co2Factor));
            })
            .OrderBy(p => p.Date)
            .ToList();

        var totalKwh = Math.Round(devices.Sum(d => d.EnergyKwh), 1);
        return new EnergyReport(
            start, end, DateTime.UtcNow, _co2Factor,
            totalKwh, EnergyMath.Co2Kg(totalKwh, _co2Factor),
            devices.Count, mills, devices, daily);
    }
}

public record EnergyReport(
    DateTime From,
    DateTime To,
    DateTime GeneratedAt,
    double Co2FactorKgPerKwh,
    double TotalEnergyKwh,
    double TotalCo2Kg,
    int MeteredDeviceCount,
    List<EnergyMillRow> Mills,
    List<EnergyDeviceRow> Devices,
    List<EnergyDailyPoint> Daily
);

public record EnergyMillRow(Guid MillId, string MillName, double EnergyKwh, double Co2Kg);

public record EnergyDeviceRow(
    Guid DeviceId, string DeviceName, string DeviceCode, string MillName,
    double AvgPowerKw, double EnergyKwh, double Co2Kg);

public record EnergyDailyPoint(string Date, double EnergyKwh, double Co2Kg);
