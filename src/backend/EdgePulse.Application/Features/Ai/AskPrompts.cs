using System.Text;

namespace EdgePulse.Application.Features.Ai;

/// <summary>
/// Prompts for the natural-language "Ask EdgePulse" feature (Sprint 30).
///
/// How grounding works here — the beginner version:
///   An LLM cannot see our database. So before we ask the model anything we
///   QUERY the data ourselves (devices, alerts, work orders the user may see),
///   render it as a short plain-text "DATA" block, and put that block in the
///   prompt together with the question. The model is then told to answer
///   ONLY from that block. This is usually called "retrieval-augmented
///   generation" (RAG) — retrieve first, generate second. It is simpler and
///   far more reliable with small local models than letting the model call
///   functions itself ("tool use"), which 3B-class models do poorly.
/// </summary>
public static class AskPrompts
{
    public const string System =
        "You are EdgePulse Assistant, a plant-monitoring helper for operators and " +
        "maintenance staff at an industrial site.\n\n" +
        "Rules:\n" +
        "- Answer ONLY from the DATA section of the message. It is the live, " +
        "authoritative state of the plant. Never invent devices, numbers, dates or causes.\n" +
        "- If the DATA does not contain what is needed, say exactly what is missing " +
        "(for example: \"I have no readings for PUMP-LW-001 in the data provided\").\n" +
        "- Refer to devices by name and code (e.g. Feed Water Pump (PUMP-LW-001)).\n" +
        "- Be concise: plain English, short sentences or a short bullet list, under 150 words.\n" +
        "- When asked for advice, hedge (\"likely\", \"consider\") — you are not a diagnosis.\n" +
        "- No preamble, no sign-off.";

    /// <summary>Builds the user prompt: DATA block + question.</summary>
    public static string ForQuestion(string dataBlock, string question, DateTime nowUtc)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Current time (UTC): {nowUtc:yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        sb.AppendLine("DATA:");
        sb.AppendLine(dataBlock.Trim());
        sb.AppendLine();
        sb.AppendLine("QUESTION:");
        sb.AppendLine(question.Trim());
        return sb.ToString();
    }
}
