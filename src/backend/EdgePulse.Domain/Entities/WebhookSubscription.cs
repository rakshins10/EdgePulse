using EdgePulse.Domain.Common;

namespace EdgePulse.Domain.Entities;

/// <summary>
/// An outbound webhook endpoint. When a subscribed event occurs the platform
/// POSTs a JSON payload signed with HMAC-SHA256 (X-EdgePulse-Signature).
/// Format "slack" sends a Slack-incoming-webhook compatible {"text": …} body.
/// </summary>
public class WebhookSubscription : TenantBaseEntity
{
    public const string FormatJson = "json";
    public const string FormatSlack = "slack";

    public string Name { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public string Secret { get; private set; } = string.Empty;
    /// <summary>Comma-separated event keys, e.g. "alert.created,workorder.created".</summary>
    public string Events { get; private set; } = string.Empty;
    public string Format { get; private set; } = FormatJson;
    public bool IsActive { get; private set; } = true;
    public string? LastStatus { get; private set; }      // e.g. "200" / "timeout"
    public DateTime? LastTriggeredAt { get; private set; }

    protected WebhookSubscription() { }

    public static WebhookSubscription Create(
        Guid tenantId, string name, string url, string secret,
        IEnumerable<string> events, string format = FormatJson)
    {
        return new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Url = url,
            Secret = secret,
            Events = string.Join(',', events.Select(e => e.Trim().ToLowerInvariant()).Distinct()),
            Format = format,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(
        string name, string url, string? secret,
        IEnumerable<string> events, string format, bool isActive)
    {
        Name = name;
        Url = url;
        if (!string.IsNullOrWhiteSpace(secret)) Secret = secret;
        Events = string.Join(',', events.Select(e => e.Trim().ToLowerInvariant()).Distinct());
        Format = format;
        IsActive = isActive;
        MarkAsUpdated();
    }

    public void RecordDelivery(string status)
    {
        LastStatus = status;
        LastTriggeredAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public bool SubscribesTo(string eventKey)
        => Events.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Contains(eventKey, StringComparer.OrdinalIgnoreCase);
}
