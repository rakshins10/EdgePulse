using EdgePulse.Application.Features.Localization.Commands;
using EdgePulse.Application.Features.Localization.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdgePulse.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class LocalizationController : ControllerBase
{
    private readonly IMediator _mediator;

    public LocalizationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ============================================================
    // LOCALES
    // ============================================================

    /// <summary>
    /// List locales. Pass enabledOnly=true for the language switcher.
    /// </summary>
    [HttpGet("locales")]
    [ProducesResponseType(typeof(List<LocaleDto>), 200)]
    public async Task<IActionResult> GetLocales(
        [FromQuery] bool enabledOnly,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetLocalesQuery(enabledOnly), cancellationToken);
        return Ok(result);
    }

    [HttpPost("locales")]
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> CreateLocale(
        [FromBody] CreateLocaleRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            new CreateLocaleCommand(
                request.Code, request.DisplayName, request.NativeName,
                request.Flag, request.IsEnabled, request.SortOrder),
            cancellationToken);
        return CreatedAtAction(nameof(GetLocales), new { }, id);
    }

    [HttpPut("locales/{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateLocale(
        Guid id,
        [FromBody] UpdateLocaleRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpdateLocaleCommand(
                id, request.DisplayName, request.NativeName,
                request.Flag, request.IsEnabled, request.SortOrder),
            cancellationToken);
        return NoContent();
    }

    [HttpDelete("locales/{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> DeleteLocale(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteLocaleCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("locales/{id:guid}/set-default")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> SetDefaultLocale(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new SetDefaultLocaleCommand(id), cancellationToken);
        return NoContent();
    }

    // ============================================================
    // LOOKUP TRANSLATIONS
    // ============================================================

    /// <summary>
    /// Get all items of a lookup type with their translation (if any) in a locale.
    /// Drives the translation editor grid.
    /// </summary>
    [HttpGet("translations")]
    [ProducesResponseType(typeof(List<LookupTranslationRowDto>), 200)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> GetTranslations(
        [FromQuery] string lookupType,
        [FromQuery] string locale,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetLookupTranslationsQuery(lookupType, locale), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create/update (or clear, when name is empty) a translation for one item.
    /// </summary>
    [HttpPut("translations")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> UpsertTranslation(
        [FromBody] UpsertTranslationRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpsertLookupTranslationCommand(
                request.LookupType, request.ItemId, request.LocaleCode,
                request.Name, request.Description),
            cancellationToken);
        return NoContent();
    }

    /// <summary>Bulk upsert lookup translations (CSV import / pre-fill).</summary>
    [HttpPut("translations/bulk")]
    [ProducesResponseType(typeof(BulkResult), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> BulkUpsertTranslations(
        [FromBody] BulkLookupRequest request,
        CancellationToken cancellationToken)
    {
        var affected = await _mediator.Send(
            new BulkUpsertLookupTranslationsCommand(
                request.LocaleCode,
                request.Entries
                    .Select(e => new LookupTranslationEntry(e.LookupType, e.ItemId, e.Name))
                    .ToList()),
            cancellationToken);
        return Ok(new BulkResult(affected));
    }

    /// <summary>All translatable lookup items with English source names.</summary>
    [HttpGet("lookup-source-items")]
    [ProducesResponseType(typeof(List<LookupSourceItemDto>), 200)]
    public async Task<IActionResult> GetLookupSourceItems(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetLookupSourceItemsQuery(), cancellationToken);
        return Ok(result);
    }

    // ============================================================
    // UI STRING OVERRIDES
    // ============================================================

    /// <summary>DB-stored UI string overrides for a locale (flat key→value map).</summary>
    [HttpGet("ui-strings")]
    [ProducesResponseType(typeof(Dictionary<string, string>), 200)]
    public async Task<IActionResult> GetUiStrings(
        [FromQuery] string locale,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUiStringsQuery(locale), cancellationToken);
        return Ok(result);
    }

    /// <summary>Bulk upsert UI string overrides (CSV import / pre-fill).</summary>
    [HttpPut("ui-strings/bulk")]
    [ProducesResponseType(typeof(BulkResult), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> BulkUpsertUiStrings(
        [FromBody] BulkUiStringsRequest request,
        CancellationToken cancellationToken)
    {
        var affected = await _mediator.Send(
            new BulkUpsertUiStringsCommand(
                request.LocaleCode,
                request.Entries.Select(e => new UiStringEntry(e.Key, e.Value)).ToList()),
            cancellationToken);
        return Ok(new BulkResult(affected));
    }
}

// Request models
public record CreateLocaleRequest(
    string Code,
    string DisplayName,
    string NativeName,
    string? Flag = null,
    bool IsEnabled = true,
    int SortOrder = 0
);

public record UpdateLocaleRequest(
    string DisplayName,
    string NativeName,
    string? Flag,
    bool IsEnabled,
    int SortOrder
);

public record UpsertTranslationRequest(
    string LookupType,
    Guid ItemId,
    string LocaleCode,
    string? Name,
    string? Description = null
);

public record BulkResult(int Affected);

public record BulkLookupRequest(string LocaleCode, List<BulkLookupEntry> Entries);
public record BulkLookupEntry(string LookupType, Guid ItemId, string? Name);

public record BulkUiStringsRequest(string LocaleCode, List<BulkUiStringEntry> Entries);
public record BulkUiStringEntry(string Key, string? Value);
