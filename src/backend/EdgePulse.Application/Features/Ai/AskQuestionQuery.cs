using System.Text;
using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Ai;

/// <summary>
/// "Ask EdgePulse" — natural-language questions about the plant, answered by
/// the LLM but GROUNDED in live data (see <see cref="AskPrompts"/> for the
/// beginner explanation of grounding / RAG).
///
/// Flow:
///   1. Work out which devices the question is about:
///        • an explicit <c>DeviceId</c> (from the device page), or
///        • device names / codes mentioned in the question text, or
///        • nothing specific → a tenant-wide snapshot.
///   2. Query ONLY data the caller is allowed to see (same role scoping as
///      the Alerts/Devices APIs: MillManager → their mill, Operator → their areas).
///   3. Render that data as a compact plain-text DATA block (a few hundred tokens).
///   4. Send DATA + question to the model; return the answer plus a
///      description of what it was grounded on, so the UI can show it.
///
/// Answers are NOT cached (every question is different) and nothing is
/// written to the database. Like alert summaries, failures never throw —
/// the result says <c>Available=false</c> with a reason.
/// </summary>
public record AskQuestionQuery(string Question, Guid? DeviceId = null)
    : IRequest<AskResult>;

public record AskResult(
    bool Available,
    string? Answer,
    string Provider,
    string? Reason,
    AskGrounding Grounding);

/// <summary>What the answer was based on — shown to the user for trust.</summary>
public record AskGrounding(
    IReadOnlyList<string> Devices,   // "Feed Water Pump (PUMP-LW-001)"
    int Alerts,
    int WorkOrders,
    string Scope);                   // "device" | "mentioned-devices" | "tenant"

public class AskQuestionQueryHandler : IRequestHandler<AskQuestionQuery, AskResult>
{
    public const int MaxQuestionLength = 500;
    private const int FocusDeviceLimit = 3;
    private const int RecentAlertsPerDevice = 5;
    private const int SnapshotAlerts = 8;
    private const int SnapshotWorkOrders = 5;

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAiAssistant _ai;

    public AskQuestionQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IAiAssistant ai)
    {
        _context = context;
        _currentUser = currentUser;
        _ai = ai;
    }

    public async Task<AskResult> Handle(AskQuestionQuery request, CancellationToken ct)
    {
        var question = (request.Question ?? string.Empty).Trim();
        if (question.Length == 0)
            throw new ValidationException(new[] {
                new ValidationFailure(nameof(request.Question), "Question is required.") });
        if (question.Length > MaxQuestionLength)
            throw new ValidationException(new[] {
                new ValidationFailure(nameof(request.Question),
                    $"Question must be {MaxQuestionLength} characters or fewer.") });

        // ---- 1. Devices the caller may see (tenant + role scoping) ----------
        var devices = _context.Devices
            .Where(d => d.TenantId == _currentUser.TenantId && !d.IsDeleted);
        if (_currentUser.IsMillManager && _currentUser.MillId.HasValue)
            devices = devices.Where(d => d.MillId == _currentUser.MillId.Value);
        if (_currentUser.IsOperator && _currentUser.AreaIds.Any())
            devices = devices.Where(d => _currentUser.AreaIds.Contains(d.AreaId));

        var catalogue = await devices
            .Select(d => new DeviceRow(d.Id, d.Name, d.Code, d.MillId, d.AreaId,
                                       d.TypeId, d.StatusId, d.LastSeenAt, d.InstallDate))
            .ToListAsync(ct);

        // ---- 2. Which devices is the question about? -----------------------
        List<DeviceRow> focus;
        string scope;
        if (request.DeviceId.HasValue)
        {
            var one = catalogue.FirstOrDefault(d => d.Id == request.DeviceId.Value)
                ?? throw new NotFoundException(nameof(Device), request.DeviceId.Value);
            focus = new List<DeviceRow> { one };
            scope = "device";
        }
        else
        {
            focus = MatchDevicesInQuestion(question, catalogue);
            scope = focus.Count > 0 ? "mentioned-devices" : "tenant";
        }

        // ---- 3. Build the DATA block ----------------------------------------
        var now = DateTime.UtcNow;
        var (data, grounding) = focus.Count > 0
            ? await BuildDeviceDataAsync(focus, now, scope, ct)
            : await BuildSnapshotDataAsync(catalogue, now, ct);

        // ---- 4. Ask the model ------------------------------------------------
        if (!_ai.IsEnabled)
            return new AskResult(false, null, _ai.Description,
                "AI assistant is not enabled on this deployment.", grounding);

        var answer = await _ai.CompleteAsync(
            AskPrompts.System, AskPrompts.ForQuestion(data, question, now), ct);

        if (string.IsNullOrWhiteSpace(answer))
            return new AskResult(false, null, _ai.Description,
                "The AI model did not return an answer (it may be starting up or overloaded). Try again.",
                grounding);

        return new AskResult(true, answer.Trim(), _ai.Description, null, grounding);
    }

    // ------------------------------------------------------------------------
    // Device matching: deterministic, cheap, no AI involved.
    // A device is "mentioned" if its code appears in the question (case-
    // insensitive) or its full name does. Codes are distinctive (PUMP-LW-001)
    // so we check them first; names are checked as whole phrases to avoid
    // matching "pump" against every pump in the plant.
    // ------------------------------------------------------------------------
    public static List<DeviceRow> MatchDevicesInQuestion(string question, List<DeviceRow> catalogue)
    {
        var q = question.ToLowerInvariant();
        var hits = new List<DeviceRow>();
        foreach (var d in catalogue.OrderByDescending(d => d.Code.Length))
        {
            if (hits.Count >= FocusDeviceLimit) break;
            if (!string.IsNullOrWhiteSpace(d.Code) && q.Contains(d.Code.ToLowerInvariant()) && !hits.Contains(d))
                hits.Add(d);
        }
        foreach (var d in catalogue.OrderByDescending(d => d.Name.Length))
        {
            if (hits.Count >= FocusDeviceLimit) break;
            if (d.Name.Length >= 4 && q.Contains(d.Name.ToLowerInvariant()) && !hits.Contains(d))
                hits.Add(d);
        }
        return hits;
    }

    private async Task<(string, AskGrounding)> BuildDeviceDataAsync(
        List<DeviceRow> focus, DateTime now, string scope, CancellationToken ct)
    {
        var ids = focus.Select(f => f.Id).ToList();
        var since = now.AddDays(-30);

        var names = await LookupNamesAsync(focus, ct);

        var alerts = await _context.Alerts
            // last 30 days, PLUS anything still open however old — an open alert
            // is always relevant to "what is wrong with this device".
            .Where(a => ids.Contains(a.DeviceId) && !a.IsDeleted &&
                        (a.TriggeredAt >= since || (a.StatusCode != "RESOLVED" && a.StatusCode != "CLOSED")))
            .OrderByDescending(a => a.TriggeredAt)
            .Select(a => new { a.DeviceId, a.MetricKey, a.TriggerValue, a.ThresholdValue,
                               a.Unit, a.SeverityCode, a.StatusCode, a.TriggeredAt })
            .ToListAsync(ct);

        var workOrders = await _context.WorkOrders
            .Where(w => ids.Contains(w.DeviceId) && !w.IsDeleted
                        && w.Status != WorkOrder.StatusCompleted && w.Status != WorkOrder.StatusCancelled)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new { w.DeviceId, w.Number, w.Title, w.Status, w.Priority, w.AssignedTo, w.DueDate })
            .ToListAsync(ct);

        var sb = new StringBuilder();
        foreach (var d in focus)
        {
            sb.AppendLine($"DEVICE: {d.Name} ({d.Code})");
            sb.AppendLine($"  type: {names.Type(d.TypeId)}; status: {names.Status(d.StatusId)}; " +
                          $"mill: {names.Mill(d.MillId)}; area: {names.Area(d.AreaId)}");
            sb.AppendLine($"  last seen: {Fmt(d.LastSeenAt)}; installed: {(d.InstallDate?.ToString("yyyy-MM-dd") ?? "unknown")}");

            var da = alerts.Where(a => a.DeviceId == d.Id).ToList();
            var open = da.Count(a => IsOpen(a.StatusCode));
            sb.AppendLine($"  alerts (last 30 days + any still open): {da.Count} total, {open} open" + SeverityBreakdown(da.Select(a => a.SeverityCode)));
            foreach (var a in da.Take(RecentAlertsPerDevice))
                sb.AppendLine($"    - {a.TriggeredAt:yyyy-MM-dd HH:mm} {a.SeverityCode} {a.MetricKey} " +
                              $"{a.TriggerValue:0.##}{a.Unit} (threshold {a.ThresholdValue:0.##}{a.Unit}), status {a.StatusCode}");

            var dw = workOrders.Where(w => w.DeviceId == d.Id).ToList();
            sb.AppendLine($"  open work orders: {dw.Count}");
            foreach (var w in dw.Take(SnapshotWorkOrders))
                sb.AppendLine($"    - {w.Number} \"{w.Title}\" {w.Status} priority {w.Priority}" +
                              (w.AssignedTo is null ? ", unassigned" : $", assigned to {w.AssignedTo}") +
                              (w.DueDate.HasValue ? $", due {w.DueDate:yyyy-MM-dd}" : ""));
        }

        var grounding = new AskGrounding(
            focus.Select(f => $"{f.Name} ({f.Code})").ToList(), alerts.Count, workOrders.Count, scope);
        return (sb.ToString(), grounding);
    }

    private async Task<(string, AskGrounding)> BuildSnapshotDataAsync(
        List<DeviceRow> catalogue, DateTime now, CancellationToken ct)
    {
        var ids = catalogue.Select(c => c.Id).ToList();
        var since7 = now.AddDays(-7);

        var openAlerts = await _context.Alerts
            .Where(a => ids.Contains(a.DeviceId) && !a.IsDeleted
                        && a.StatusCode != "RESOLVED" && a.StatusCode != "CLOSED")
            .OrderByDescending(a => a.TriggeredAt)
            .Select(a => new { a.DeviceId, a.MetricKey, a.TriggerValue, a.ThresholdValue,
                               a.Unit, a.SeverityCode, a.StatusCode, a.TriggeredAt })
            .ToListAsync(ct);

        var weekCounts = await _context.Alerts
            .Where(a => ids.Contains(a.DeviceId) && !a.IsDeleted && a.TriggeredAt >= since7)
            .GroupBy(a => a.DeviceId)
            .Select(g => new { DeviceId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count).Take(3)
            .ToListAsync(ct);

        var openWo = await _context.WorkOrders
            .Where(w => ids.Contains(w.DeviceId) && !w.IsDeleted
                        && w.Status != WorkOrder.StatusCompleted && w.Status != WorkOrder.StatusCancelled)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new { w.DeviceId, w.Number, w.Title, w.Status, w.Priority, w.AssignedTo })
            .ToListAsync(ct);

        string Dev(Guid id)
        {
            var d = catalogue.FirstOrDefault(c => c.Id == id);
            return d is null ? "unknown device" : $"{d.Name} ({d.Code})";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"PLANT SNAPSHOT (devices visible to you: {catalogue.Count})");
        sb.AppendLine($"Open alerts: {openAlerts.Count}" + SeverityBreakdown(openAlerts.Select(a => a.SeverityCode)));
        foreach (var a in openAlerts.Take(SnapshotAlerts))
            sb.AppendLine($"  - {a.TriggeredAt:yyyy-MM-dd HH:mm} {a.SeverityCode} {Dev(a.DeviceId)} {a.MetricKey} " +
                          $"{a.TriggerValue:0.##}{a.Unit} (threshold {a.ThresholdValue:0.##}{a.Unit}), status {a.StatusCode}");
        if (openAlerts.Count > SnapshotAlerts)
            sb.AppendLine($"  ... and {openAlerts.Count - SnapshotAlerts} more open alerts");

        if (weekCounts.Count > 0)
            sb.AppendLine("Most alerts in the last 7 days: " +
                string.Join(", ", weekCounts.Select(w => $"{Dev(w.DeviceId)} {w.Count}")));

        sb.AppendLine($"Open work orders: {openWo.Count}");
        foreach (var w in openWo.Take(SnapshotWorkOrders))
            sb.AppendLine($"  - {w.Number} \"{w.Title}\" on {Dev(w.DeviceId)}, {w.Status}, priority {w.Priority}" +
                          (w.AssignedTo is null ? ", unassigned" : $", assigned to {w.AssignedTo}"));

        var grounding = new AskGrounding(Array.Empty<string>(), openAlerts.Count, openWo.Count, "tenant");
        return (sb.ToString(), grounding);
    }

    private async Task<Names> LookupNamesAsync(List<DeviceRow> focus, CancellationToken ct)
    {
        var typeIds = focus.Select(f => f.TypeId).Distinct().ToList();
        var statusIds = focus.Select(f => f.StatusId).Distinct().ToList();
        var millIds = focus.Select(f => f.MillId).Distinct().ToList();
        var areaIds = focus.Select(f => f.AreaId).Distinct().ToList();
        return new Names(
            await _context.DeviceTypes.Where(t => typeIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Name, ct),
            await _context.DeviceStatuses.Where(s => statusIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, s => s.Name, ct),
            await _context.Mills.Where(m => millIds.Contains(m.Id)).ToDictionaryAsync(m => m.Id, m => m.Name, ct),
            await _context.Areas.Where(a => areaIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, a => a.Name, ct));
    }

    private static bool IsOpen(string status) => status != "RESOLVED" && status != "CLOSED";

    private static string SeverityBreakdown(IEnumerable<string> severities)
    {
        var groups = severities.GroupBy(s => s).OrderBy(g => SeverityRank(g.Key)).ToList();
        return groups.Count == 0 ? "" :
            " (" + string.Join(", ", groups.Select(g => $"{g.Count()} {g.Key}")) + ")";
    }

    private static string Fmt(DateTime? t) => t.HasValue ? t.Value.ToString("yyyy-MM-dd HH:mm") + " UTC" : "never";

    private static int SeverityRank(string code) => code switch
    {
        "CRITICAL" => 0, "HIGH" => 1, "MEDIUM" => 2, "LOW" => 3, _ => 4
    };

    public record DeviceRow(Guid Id, string Name, string Code, Guid MillId, Guid AreaId,
                              Guid TypeId, Guid StatusId, DateTime? LastSeenAt, DateOnly? InstallDate);

    private record Names(
        Dictionary<Guid, string> Types, Dictionary<Guid, string> Statuses,
        Dictionary<Guid, string> Mills, Dictionary<Guid, string> Areas)
    {
        public string Type(Guid id) => Types.GetValueOrDefault(id, "unknown");
        public string Status(Guid id) => Statuses.GetValueOrDefault(id, "unknown");
        public string Mill(Guid id) => Mills.GetValueOrDefault(id, "unknown");
        public string Area(Guid id) => Areas.GetValueOrDefault(id, "unknown");
    }
}
