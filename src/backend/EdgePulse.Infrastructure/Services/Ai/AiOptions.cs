namespace EdgePulse.Infrastructure.Services.Ai;

/// <summary>
/// Bound from the "Ai" configuration section.
///
///   "Ai": {
///     "Provider": "ollama",                     // ollama | azureopenai | none
///     "Ollama":  { "BaseUrl": "http://localhost:11434", "Model": "llama3.2" },
///     "AzureOpenAi": { "Endpoint": "https://x.openai.azure.com",
///                      "Deployment": "gpt-4o-mini", "ApiKey": "<secret>" },
///     "TimeoutSeconds": 60
///   }
/// </summary>
public class AiOptions
{
    public string Provider { get; set; } = "none";
    public int TimeoutSeconds { get; set; } = 60;
    public OllamaOptions Ollama { get; set; } = new();
    public AzureOpenAiOptions AzureOpenAi { get; set; } = new();

    public class OllamaOptions
    {
        public string BaseUrl { get; set; } = "http://localhost:11434";
        public string Model { get; set; } = "llama3.2";
    }

    public class AzureOpenAiOptions
    {
        public string Endpoint { get; set; } = string.Empty;
        public string Deployment { get; set; } = "gpt-4o-mini";
        public string ApiKey { get; set; } = string.Empty;
        public string ApiVersion { get; set; } = "2024-10-21";
    }
}
