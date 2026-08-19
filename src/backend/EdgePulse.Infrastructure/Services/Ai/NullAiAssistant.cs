using EdgePulse.Application.Common.Interfaces;

namespace EdgePulse.Infrastructure.Services.Ai;

/// <summary>
/// Used when Ai:Provider is "none" (or unrecognised). Lets every handler that
/// depends on IAiAssistant run unchanged — AI simply reports itself disabled.
/// This is what makes AI a true opt-in feature.
/// </summary>
public class NullAiAssistant : IAiAssistant
{
    public bool IsEnabled => false;
    public string Description => "disabled";
    public Task<string?> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct)
        => Task.FromResult<string?>(null);
}
