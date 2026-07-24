using System.Text;
using EdgePulse.Application.Common;
using EdgePulse.Application.Features.Reports.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdgePulse.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Cross-mill comparison over a date range (defaults: last 30 days).
    /// Devices, alert volumes, severities and MTTA / MTTR per mill.
    /// </summary>
    [HttpGet("mill-comparison")]
    [ProducesResponseType(typeof(MillComparisonReport), 200)]
    public async Task<IActionResult> GetMillComparison(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var (f, t) = Range(from, to);
        var result = await _mediator.Send(
            new GetMillComparisonReportQuery(f, t), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// The comparison report as a CSV download.
    /// </summary>
    [HttpGet("mill-comparison/csv")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetMillComparisonCsv(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var (f, t) = Range(from, to);
        var report = await _mediator.Send(
            new GetMillComparisonReportQuery(f, t), cancellationToken);

        var csv = CsvBuilder.Build(
            ["Mill", "Location", "Devices", "Alerts", "Open", "Critical",
             "High", "Avg ack (min)", "Avg resolve (min)"],
            report.Mills.Select(m => (IEnumerable<object?>)
            [
                m.MillName, m.Location, m.DeviceCount, m.TotalAlerts,
                m.OpenAlerts, m.CriticalAlerts, m.HighAlerts,
                m.AvgAcknowledgeMinutes, m.AvgResolveMinutes
            ]));

        return CsvFile(csv, $"mill-comparison_{f:yyyyMMdd}-{t:yyyyMMdd}.csv");
    }

    /// <summary>
    /// Full alert detail export for the range as CSV.
    /// </summary>
    [HttpGet("alerts/csv")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetAlertsCsv(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var (f, t) = Range(from, to);
        var csv = await _mediator.Send(
            new ExportAlertsCsvQuery(f, t), cancellationToken);
        return CsvFile(csv, $"alerts_{f:yyyyMMdd}-{t:yyyyMMdd}.csv");
    }

    private static (DateTime From, DateTime To) Range(DateTime? from, DateTime? to)
    {
        var t = to?.ToUniversalTime() ?? DateTime.UtcNow;
        var f = from?.ToUniversalTime() ?? t.AddDays(-30);
        return (f, t);
    }

    private FileContentResult CsvFile(string csv, string fileName)
        // UTF-8 BOM so Excel detects the encoding
        => File(
            Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray(),
            "text/csv; charset=utf-8",
            fileName);
}
