using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Security.Claims;

namespace EdgePulse.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TelemetryController : ControllerBase
{
    private readonly IMongoCollection<TelemetryReadingDocument> _readings;

    public TelemetryController(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MongoDB")
            ?? throw new InvalidOperationException("ConnectionStrings:MongoDB is not configured.");
        var database = configuration["MongoDB:Database"] ?? "edgepulse_telemetry";
        var client = new MongoClient(connectionString);
        _readings = client.GetDatabase(database).GetCollection<TelemetryReadingDocument>("telemetry_readings");
    }

    /// <summary>
    /// Get telemetry readings for a device, scoped to the caller's tenant.
    /// Ordered newest-first. Max 1000, default 100.
    /// </summary>
    [HttpGet("devices/{deviceId:guid}")]
    [ProducesResponseType(typeof(TelemetryResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetDeviceTelemetry(
        Guid deviceId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var tenantIdClaim = User.FindFirstValue("tenantId");
        if (!Guid.TryParse(tenantIdClaim, out var tenantId))
            return Unauthorized();

        limit = Math.Clamp(limit, 1, 1000);

        var fb = Builders<TelemetryReadingDocument>.Filter;
        var filter = fb.Eq(r => r.DeviceId, deviceId) & fb.Eq(r => r.TenantId, tenantId);
        if (from.HasValue) filter &= fb.Gte(r => r.Timestamp, from.Value.ToUniversalTime());
        if (to.HasValue)   filter &= fb.Lte(r => r.Timestamp, to.Value.ToUniversalTime());

        var docs = await _readings
            .Find(filter)
            .Sort(Builders<TelemetryReadingDocument>.Sort.Descending(r => r.Timestamp))
            .Limit(limit)
            .ToListAsync(cancellationToken);

        return Ok(new TelemetryResponse(
            DeviceId: deviceId,
            Count: docs.Count,
            Readings: docs.Select(r => new TelemetryReadingDto(
                r.Timestamp,
                r.Metrics.Select(m => new MetricDto(m.Key, m.Value, m.Unit)).ToList()
            )).ToList()
        ));
    }
}

// ── Response DTOs ─────────────────────────────────────────────────────────────

public record TelemetryResponse(Guid DeviceId, int Count, List<TelemetryReadingDto> Readings);
public record TelemetryReadingDto(DateTime Timestamp, List<MetricDto> Metrics);
public record MetricDto(string Key, double Value, string? Unit);

// ── MongoDB document model ────────────────────────────────────────────────────
// Field names must match what TelemetryProcessor writes (PascalCase by default
// in MongoDB.Driver 3.x — verify by checking a document in mongosh).

[BsonIgnoreExtraElements]
public class TelemetryReadingDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public Guid DeviceId  { get; set; }
    public Guid TenantId  { get; set; }
    public Guid MillId    { get; set; }
    public Guid AreaId    { get; set; }
    public DateTime Timestamp { get; set; }
    public List<MetricDocument> Metrics { get; set; } = [];
}

[BsonIgnoreExtraElements]
public class MetricDocument
{
    public string Key   { get; set; } = string.Empty;
    public double Value { get; set; }
    public string? Unit { get; set; }
}
