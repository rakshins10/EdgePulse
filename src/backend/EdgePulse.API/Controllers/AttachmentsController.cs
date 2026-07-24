using EdgePulse.Application.Features.Attachments.Commands;
using EdgePulse.Application.Features.Attachments.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdgePulse.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AttachmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AttachmentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// List attachments for an entity (Device, Mill or Area).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<AttachmentDto>), 200)]
    public async Task<IActionResult> GetAttachments(
        [FromQuery] string entityType,
        [FromQuery] Guid entityId,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetAttachmentsQuery(entityType, entityId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Upload a file attachment (multipart/form-data).
    /// Max 25 MB; documents, images and CAD formats only.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(26_214_400)] // 25 MB + form overhead
    [ProducesResponseType(typeof(AttachmentDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Upload(
        [FromForm] string entityType,
        [FromForm] Guid entityId,
        [FromForm] string? category,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        await using var stream = file.OpenReadStream();
        var result = await _mediator.Send(
            new UploadAttachmentCommand(
                entityType, entityId,
                file.FileName, file.ContentType, file.Length,
                stream, string.IsNullOrWhiteSpace(category) ? "General" : category),
            cancellationToken);
        return CreatedAtAction(
            nameof(GetAttachments),
            new { entityType, entityId },
            result);
    }

    /// <summary>
    /// Download the original file.
    /// </summary>
    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Download(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new DownloadAttachmentQuery(id), cancellationToken);
        return File(result.Content, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Delete an attachment (soft-deletes the record, removes the file).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new DeleteAttachmentCommand(id), cancellationToken);
        return NoContent();
    }
}
