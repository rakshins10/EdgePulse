using EdgePulse.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace EdgePulse.Infrastructure.Services;

/// <summary>
/// Resolves the caller's locale from the Accept-Language header. The frontend
/// sends the active UI language (e.g. "fi") via an axios interceptor. We take
/// only the primary subtag (so "fi-FI,fi;q=0.9" → "fi") and default to "en".
/// </summary>
public class LocaleContext : ILocaleContext
{
    private const string DefaultLocale = "en";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LocaleContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string CurrentLocale
    {
        get
        {
            var header = _httpContextAccessor.HttpContext?
                .Request.Headers["Accept-Language"].ToString();

            if (string.IsNullOrWhiteSpace(header))
                return DefaultLocale;

            // Take the first language tag, drop any q-weight and region subtag.
            var first = header.Split(',')[0].Split(';')[0].Trim();
            if (string.IsNullOrEmpty(first))
                return DefaultLocale;

            var primary = first.Split('-')[0].ToLowerInvariant();
            return string.IsNullOrEmpty(primary) ? DefaultLocale : primary;
        }
    }
}
