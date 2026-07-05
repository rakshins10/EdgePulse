namespace EdgePulse.Application.Common.Interfaces;

/// <summary>A resolved translation for a single lookup item.</summary>
public record LookupTranslationValue(string Name, string? Description);

/// <summary>
/// Resolves translated names/descriptions for lookup items in the caller's
/// current locale. Returns an empty map when the current locale is the default
/// ("en") or no translations exist — callers then keep the item's stored name.
/// </summary>
public interface ILookupTranslator
{
    /// <summary>
    /// Returns a map of itemId → translation for the given lookup type in the
    /// caller's current locale. Tenant-specific translations take precedence
    /// over global ones. Empty when locale is default or nothing is translated.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, LookupTranslationValue>> GetMapAsync(
        string lookupType,
        CancellationToken cancellationToken = default);
}
