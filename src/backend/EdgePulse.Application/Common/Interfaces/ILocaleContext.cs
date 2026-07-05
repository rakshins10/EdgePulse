namespace EdgePulse.Application.Common.Interfaces;

/// <summary>
/// Surfaces the locale requested by the current caller (from the
/// Accept-Language header). Used by lookup query handlers to resolve
/// translated item names. Defaults to "en" when no header is present.
/// </summary>
public interface ILocaleContext
{
    /// <summary>BCP-47 language code, lowercased, e.g. "en", "fi", "sv".</summary>
    string CurrentLocale { get; }
}
