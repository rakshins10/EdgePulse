using System.Text;
using System.Text.Json;
using EdgePulse.Application.Common;
using EdgePulse.Application.Features.Webhooks;
using EdgePulse.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace EdgePulse.Infrastructure.Services;

/// <summary>
/// Delivers signed webhook POSTs. Payload:
///   { "event": "...", "timestamp": "...", "data": { ... } }
/// signed with HMAC-SHA256 in X-EdgePulse-Signature. Format "slack" sends a
/// Slack-incoming-webhook compatible {"text": "..."} body instead.
/// </summary>
public class WebhookSender : IWebhookSender
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ILogger<WebhookSender> _logger;

    public WebhookSender(HttpClient http, ILogger<WebhookSender> logger)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(10);
        _logger = logger;
    }

    public async Task<string> SendAsync(
        WebhookSubscription subscription,
        string eventKey,
        object data,
        CancellationToken cancellationToken)
    {
        try
        {
            string body;
            if (subscription.Format == WebhookSubscription.FormatSlack)
            {
                var text = $":zap: EdgePulse `{eventKey}` — " +
                    JsonSerializer.Serialize(data, JsonOptions);
                body = JsonSerializer.Serialize(new { text }, JsonOptions);
            }
            else
            {
                body = JsonSerializer.Serialize(new
                {
                    @event = eventKey,
                    timestamp = DateTime.UtcNow,
                    data,
                }, JsonOptions);
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, subscription.Url)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            request.Headers.Add(WebhookSigner.HeaderName,
                WebhookSigner.Sign(subscription.Secret, body));
            request.Headers.Add("X-EdgePulse-Event", eventKey);

            var response = await _http.SendAsync(request, cancellationToken);
            return ((int)response.StatusCode).ToString();
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Webhook '{Name}' timed out", subscription.Name);
            return "error: timeout";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Webhook '{Name}' delivery failed", subscription.Name);
            return $"error: {ex.GetType().Name}";
        }
    }
}
