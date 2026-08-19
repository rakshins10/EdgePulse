namespace EdgePulse.Application.Features.Ai;

/// <summary>
/// The prompts used to turn a raw alert into a plain-language summary.
///
/// HOW PROMPTING WORKS (for readers new to LLMs):
/// A language model predicts text. You steer it with two pieces of text:
///   1. The SYSTEM prompt — persistent instructions: who the model is, what
///      format to answer in, what it must never do. Think "job description".
///   2. The USER prompt — the actual request, with the data it needs.
/// The model then writes a continuation that fits both. Good prompts are
/// specific about FORMAT (so output is predictable and parseable), give the
/// model the FACTS it needs (so it doesn't invent them), and say what to do
/// when it DOESN'T know (so it says so instead of guessing).
///
/// Everything the model sees is built here, so it is easy to review and tune.
/// </summary>
public static class AlertSummaryPrompts
{
    /// <summary>
    /// System prompt: fixes the persona, the audience, the output shape and the
    /// guard-rails. Kept short — small on-prem models follow short, concrete
    /// instructions far more reliably than long ones.
    /// </summary>
    public const string System =
        """
        You are a maintenance assistant for an industrial plant. You explain
        equipment alerts to shift operators and maintenance technicians.

        Rules:
        - Write in plain English, 3 short sections with these exact headings:
          WHAT HAPPENED: one sentence stating the fact.
          LIKELY CAUSES: 2-3 bullet points, most probable first.
          RECOMMENDED ACTION: 2-3 bullet points the technician should do now.
        - Use only the data you are given. If you are not sure, say
          "likely" or "possible" — never state a cause as certain.
        - No preamble, no sign-off, no markdown other than the bullets.
        - Keep the whole answer under 120 words.
        """;

    /// <summary>
    /// User prompt: the alert facts, formatted as labelled fields so the model
    /// cannot confuse the reading with the threshold. Recent readings (if any)
    /// let it comment on trend ("rising steadily" vs "sudden spike").
    /// </summary>
    public static string ForAlert(
        string deviceName,
        string deviceCode,
        string deviceType,
        string metricKey,
        double triggerValue,
        double thresholdValue,
        string? unit,
        string severity,
        DateTime triggeredAt,
        IReadOnlyList<double>? recentValues)
    {
        var u = string.IsNullOrEmpty(unit) ? "" : $" {unit}";
        var trend = recentValues is { Count: > 1 }
            ? $"\nRecent readings (oldest to newest): {string.Join(", ", recentValues.Select(v => $"{v:0.##}{u}"))}"
            : "";

        return
            $"""
            Explain this alert.

            Device: {deviceName} ({deviceCode}), type: {deviceType}
            Metric: {metricKey}
            Measured value: {triggerValue:0.##}{u}
            Alert threshold: {thresholdValue:0.##}{u}
            Severity: {severity}
            Triggered at: {triggeredAt:yyyy-MM-dd HH:mm} UTC{trend}
            """;
    }
}
