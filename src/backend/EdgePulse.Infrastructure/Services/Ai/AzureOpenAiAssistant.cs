using System.Net.Http.Json;
using System.Text.Json.Serialization;
using EdgePulse.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EdgePulse.Infrastructure.Services.Ai;

/// <summary>
/// IAiAssistant backed by Azure OpenAI (the cloud deployment profile).
/// Same message shape as Ollama — the difference is the URL pattern,
/// the api-key header and that a "deployment" name replaces the model name.
/// Selected with Ai:Provider = "azureopenai"; the API key must come from
/// user-secrets / environment, never from a committed file.
/// </summary>
public class AzureOpenAiAssistant : IAiAssistant
{
    private readonly HttpClient _http;
    private readonly AiOptions _options;
    private readonly ILogger<AzureOpenAiAssistant> _logger;

    public AzureOpenAiAssistant(
        HttpClient http, IOptions<AiOptions> options, ILogger<AzureOpenAiAssistant> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
        _http.Timeout = TimeSpan.FromSeconds(Math.Max(10, _options.TimeoutSeconds));
        if (!string.IsNullOrEmpty(_options.AzureOpenAi.ApiKey))
            _http.DefaultRequestHeaders.Add("api-key", _options.AzureOpenAi.ApiKey);
    }

    public bool IsEnabled =>
        !string.IsNullOrWhiteSpace(_options.AzureOpenAi.Endpoint) &&
        !string.IsNullOrWhiteSpace(_options.AzureOpenAi.ApiKey) &&
        !_options.AzureOpenAi.ApiKey.Contains("<SET-VIA-");

    public string Description => $"azure-openai/{_options.AzureOpenAi.Deployment}";

    public async Task<string?> CompleteAsync(
        string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        if (!IsEnabled) return null;
        try
        {
            var url = $"{_options.AzureOpenAi.Endpoint.TrimEnd('/')}/openai/deployments/" +
                      $"{_options.AzureOpenAi.Deployment}/chat/completions" +
                      $"?api-version={_options.AzureOpenAi.ApiVersion}";

            var request = new
            {
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt },
                },
                temperature = 0.2,
                max_tokens = 300,
            };

            using var response = await _http.PostAsJsonAsync(url, request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Azure OpenAI returned {Status}", (int)response.StatusCode);
                return null;
            }
            var result = await response.Content.ReadFromJsonAsync<Completion>(cancellationToken);
            return result?.Choices?.FirstOrDefault()?.Message?.Content;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Azure OpenAI call failed");
            return null;
        }
    }

    private sealed class Completion
    {
        [JsonPropertyName("choices")] public List<Choice>? Choices { get; set; }
    }
    private sealed class Choice
    {
        [JsonPropertyName("message")] public Msg? Message { get; set; }
    }
    private sealed class Msg
    {
        [JsonPropertyName("content")] public string? Content { get; set; }
    }
}
