using EdgePulse.Application.Features.Dashboard.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdgePulse.API.Controllers;

/// <summary>
/// Provides the executive dashboard summary.
/// Accessible to CustomerAdmin, MillManager, Operator, and Executive roles.
/// All data is scoped to the caller's tenant (and mill/area for lower roles).
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get the executive dashboard summary.
    ///
    /// Returns:
    ///   - KPI counts: total devices, open alerts, critical alerts, devices with alerts
    ///   - 7-day alert trend (one entry per calendar day, UTC, oldest first)
    ///   - Alert distribution by severity (CRITICAL / HIGH / MEDIUM / LOW)
    ///   - Top 5 devices ranked by active alert count
    ///
    /// Role scoping:
    ///   - SuperAdmin / CustomerAdmin: entire tenant
    ///   - MillManager: their assigned mill only
    ///   - Operator: their assigned areas only
    ///   - Executive: entire tenant (read-only view)
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetSummary(
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetDashboardSummaryQuery(),
            cancellationToken);

        return Ok(result);
    }
}
