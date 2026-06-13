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
