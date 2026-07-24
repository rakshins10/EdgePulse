using System.Text;
using EdgePulse.Application.Common;
using EdgePulse.Application.Features.Audit;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdgePulse.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AuditController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuditController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Audit trail (admin only). Newest first, filterable.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<AuditLogDto>), 200)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? entityType,
        [FromQuery] string? action,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int take = 200,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetAuditLogsQuery(entityType, action, from, to, take),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Audit trail as CSV (admin only) — for compliance evidence packs.
    /// </summary>
    [HttpGet("csv")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetAuditCsv(
        [FromQuery] string? entityType,
        [FromQuery] string? action,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var rows = await _mediator.Send(
            new GetAuditLogsQuery(entityType, action, from, to, Take: 1000),
            cancellationToken);

        var csv = CsvBuilder.Build(
            ["Timestamp (UTC)", "User", "Action", "Entity", "EntityId", "Name", "Changes"],
            rows.Select(r => (IEnumerable<object?>)
                [r.Timestamp, r.UserName, r.Action, r.EntityType,
                 r.EntityId, r.EntityDisplay, r.ChangesJson]));

        return File(
            Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray(),
            "text/csv; charset=utf-8",
            $"audit-trail_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
