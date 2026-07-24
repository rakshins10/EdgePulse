using System.Security.Cryptography;
using System.Text;

namespace EdgePulse.Application.Common;

/// <summary>
/// HMAC-SHA256 request signing for outbound webhooks. Receivers verify by
/// recomputing the hex digest of the raw body with the shared secret and
/// comparing against the X-EdgePulse-Signature header.
/// </summary>
public static class WebhookSigner
{
    public const string HeaderName = "X-EdgePulse-Signature";

    public static string Sign(string secret, string body)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
