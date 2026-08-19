namespace EdgePulse.Application.Common.Interfaces;

/// <summary>
/// A chat-style language-model provider. One method, one job: given a system
/// prompt (who the model is / the rules) and a user prompt (the actual
/// request + data), return the model's text.
///
/// Implementations live in Infrastructure:
///   - OllamaAiAssistant      — on-premise, talks to a local Ollama server
///   - AzureOpenAiAssistant   — cloud, talks to Azure OpenAI
///   - NullAiAssistant        — used when AI is disabled; always "unavailable"
///
/// Handlers never know which one is active — it is chosen by configuration
/// (Ai:Provider). That is what lets the same code run in a mill with no
/// internet and in an Azure tenant.
/// </summary>
public interface IAiAssistant
{
    /// <summary>True when a provider is configured and reachable enough to try.</summary>
    bool IsEnabled { get; }

    /// <summary>Human-readable provider + model, e.g. "ollama/llama3.2" — for the UI.</summary>
    string Description { get; }

    /// <summary>
    /// Generate a completion. Returns null if the provider is disabled or the
    /// call fails — callers must treat null as "no AI answer", never as an error
    /// that should break the feature around it.
    /// </summary>
    Task<string?> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken);
}
