using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EdgePulse.TelemetryProcessor.Services;

/// <summary>
/// Fires tenant webhook subscriptions for engine events (alert.created,
/// workorder.created). Kept dependency-free like the rest of this worker:
/// raw SQL for subscriptions, HttpClient for delivery, inline HMAC-SHA256
/// signing (same X-EdgePulse-Signature scheme as the API's WebhookSender).
/// Best-effort: failures are logged and recorded on the subscription.
/// </summary>
public class WebhookDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly string _sqlConnectionString;
    private readonly HttpClient _http;
    private readonly ILogger<WebhookDispatcher> _logger;

    public WebhookDispatcher(
        string sqlConnectionString,
        ILogger<WebhookDispatcher> logger)
    {
        _sqlConnectionString = sqlConnectionString;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _logger = logger;
    }

    public async Task DispatchAsync(
        Guid tenantId, string eventKey, object data, CancellationToken ct)
    {
        try
        {
            var subscriptions = await LoadSubscriptionsAsync(tenantId, eventKey, ct);
            foreach (var sub in subscriptions)
            {
                var status = await SendAsync(sub, eventKey, data, ct);
                await RecordDeliveryAsync(sub.Id, status, ct);
                _logger.LogInformation(
                    "Webhook '{Name}' ({Event}) -> {Status}", sub.Name, eventKey, status);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook dispatch failed for {Event}", eventKey);
        }
    }

    private async Task<List<Subscription>> LoadSubscriptionsAsync(
        Guid tenantId, string eventKey, CancellationToken ct)
    {
        var result = new List<Subscription>();
        await using var conn = new SqlConnection(_sqlConnectionString);
        await conn.OpenAsync(ct);

        const string sql = """
            SELECT Id, Name, Url, Secret, Events, Format
            FROM   WebhookSubscriptions
            WHERE  TenantId = @TenantId AND IsActive = 1 AND IsDeleted = 0
            """;
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@TenantId", tenantId);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var events = reader.GetString(4)
                .Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (!events.Contains(eventKey, StringComparer.OrdinalIgnoreCase)) continue;
            result.Add(new Subscription(
                reader.GetGuid(0), reader.GetString(1), reader.GetString(2),
                reader.GetString(3), reader.GetString(5)));
        }
        return result;
    }

    private async Task<string> SendAsync(
        Subscription sub, string eventKey, object data, CancellationToken ct)
    {
        try
        {
            string body = sub.Format == "slack"
                ? JsonSerializer.Serialize(new
                  {
                      text = $":zap: EdgePulse `{eventKey}` — " +
                             JsonSerializer.Serialize(data, JsonOptions)
                  }, JsonOptions)
                : JsonSerializer.Serialize(new
                  {
                      @event = eventKey,
                      timestamp = DateTime.UtcNow,
                      data,
                  }, JsonOptions);

            using var request = new HttpRequestMessage(HttpMethod.Post, sub.Url)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(sub.Secret));
            request.Headers.Add("X-EdgePulse-Signature",
                Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body)))
                    .ToLowerInvariant());
            request.Headers.Add("X-EdgePulse-Event", eventKey);

            var response = await _http.SendAsync(request, ct);
            return ((int)response.StatusCode).ToString();
        }
        catch (TaskCanceledException) { return "error: timeout"; }
        catch (Exception ex) { return $"error: {ex.GetType().Name}"; }
    }

    private async Task RecordDeliveryAsync(Guid id, string status, CancellationToken ct)
    {
        try
        {
            await using var conn = new SqlConnection(_sqlConnectionString);
            await conn.OpenAsync(ct);
            const string sql = """
                UPDATE WebhookSubscriptions
                SET    LastStatus = @Status, LastTriggeredAt = @Now, UpdatedAt = @Now
                WHERE  Id = @Id
                """;
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@Status",
                status.Length > 50 ? status[..50] : status);
            cmd.Parameters.AddWithValue("@Now", DateTime.UtcNow);
            await cmd.ExecuteNonQueryAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record webhook delivery status");
        }
    }

    private sealed record Subscription(
        Guid Id, string Name, string Url, string Secret, string Format);
}
