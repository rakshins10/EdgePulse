using System.Text.Json;
using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Ai;

/// <summary>
/// Returns a plain-language AI summary for one alert.
///
/// Design decisions (worth knowing):
///   • ON DEMAND, not at alert time. An LLM takes seconds; the alert engine
///     must never wait on it. Summaries are produced the first time someone
///     asks for one.
///   • CACHED on Alert.AiSummary. Generated once, then served from the DB —
///     no repeated model calls, and the text is stable for the audit trail.
///   • NEVER FAILS THE CALLER. If AI is disabled or the model is down, the
///     response says so (<c>Available=false</c>) and the alert is untouched.
///   • Pass <c>Regenerate=true</c> to discard the cached text and ask again.
/// </summary>
public record GetAlertSummaryQuery(Guid AlertId, bool Regenerate = false)
    : IRequest<AlertSummaryResult>;

public record AlertSummaryResult(
    Guid AlertId,
    bool Available,          // false = AI disabled / unreachable / failed
    string? Summary,
    bool FromCache,
    string Provider,         // e.g. "ollama/llama3.2" or "disabled"
    string? Reason           // why unavailable, for the UI
);

public class GetAlertSummaryQueryHandler
    : IRequestHandler<GetAlertSummaryQuery, AlertSummaryResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAiAssistant _ai;

    public GetAlertSummaryQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IAiAssistant ai)
    {
        _context = context;
        _currentUser = currentUser;
        _ai = ai;
    }

    public async Task<AlertSummaryResult> Handle(
        GetAlertSummaryQuery request, CancellationToken cancellationToken)
    {
        var alert = await _context.Alerts
            .FirstOrDefaultAsync(a =>
                a.Id == request.AlertId &&
                a.TenantId == _currentUser.TenantId && !a.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException(nameof(Alert), request.AlertId);

        // 1. Cache hit
        if (!request.Regenerate && !string.IsNullOrWhiteSpace(alert.AiSummary))
            return new AlertSummaryResult(
                alert.Id, true, alert.AiSummary, FromCache: true, _ai.Description, null);

        // 2. AI switched off → honest "unavailable", no error
        if (!_ai.IsEnabled)
            return new AlertSummaryResult(
                alert.Id, false, null, false, _ai.Description,
                "AI assistant is not enabled on this deployment.");

        // 3. Gather the facts the model needs (device name/type for context)
        var device = await _context.Devices
            .Where(d => d.Id == alert.DeviceId)
            .Join(_context.DeviceTypes, d => d.TypeId, t => t.Id,
                (d, t) => new { d.Name, d.Code, TypeName = t.Name })
            .FirstOrDefaultAsync(cancellationToken);

        var recent = ParseRecentValues(alert.ReadingsJson);

        var userPrompt = AlertSummaryPrompts.ForAlert(
            device?.Name ?? "Unknown device",
            device?.Code ?? "",
            device?.TypeName ?? "equipment",
            alert.MetricKey,
            alert.TriggerValue,
            alert.ThresholdValue,
            alert.Unit,
            alert.SeverityCode,
            alert.TriggeredAt,
            recent);

        // 4. Ask the model. Null = it failed; we report, we don't throw.
        var text = await _ai.CompleteAsync(
            AlertSummaryPrompts.System, userPrompt, cancellationToken);

        if (string.IsNullOrWhiteSpace(text))
            return new AlertSummaryResult(
                alert.Id, false, null, false, _ai.Description,
                "The AI model did not return a summary (it may be starting up or overloaded). Try again.");

        // 5. Cache on the alert
        alert.SetAiSummary(text.Trim());
        _context.Update(alert);
        await _context.SaveChangesAsync(cancellationToken);

        return new AlertSummaryResult(
            alert.Id, true, alert.AiSummary, FromCache: false, _ai.Description, null);
    }

    /// <summary>
    /// ReadingsJson is the snapshot the engine stored at fire time:
    /// [{"Timestamp":...,"Value":...}, ...]. Extract the values for trend context.
    /// </summary>
    private static IReadOnlyList<double>? ParseRecentValues(string? readingsJson)
    {
        if (string.IsNullOrWhiteSpace(readingsJson)) return null;
        try
        {
            using var doc = JsonDocument.Parse(readingsJson);
            var values = new List<double>();
            foreach (var item in doc.RootElement.EnumerateArray())
                if (item.TryGetProperty("Value", out var v) && v.TryGetDouble(out var d))
                    values.Add(d);
            return values.Count > 0 ? values : null;
        }
        catch
        {
            return null; // malformed snapshot is not worth failing a summary over
        }
    }
}
