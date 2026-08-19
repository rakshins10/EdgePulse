using System.Net.Http.Json;
using System.Text.Json.Serialization;
using EdgePulse.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EdgePulse.Infrastructure.Services.Ai;

/// <summary>
/// IAiAssistant backed by a local Ollama server.
///
/// WHAT OLLAMA IS: an open-source program that downloads open language models
/// (Llama, Mistral, …) and serves them over a small HTTP API on your own
/// machine — no account, no API key, no data leaving the network. We run it as
/// a Docker container next to SQL Server and talk to it like any other service.
///
/// THE CALL: POST {BaseUrl}/api/chat with the model name and a list of
/// messages (role = system | user | assistant). With "stream": false Ollama
/// returns one JSON object whose message.content is the whole answer.
/// Ollama's API is deliberately OpenAI-shaped, so swapping providers is mostly
/// a URL + auth change.
/// </summary>
public class OllamaAiAssistant : IAiAssistant
{
    private readonly HttpClient _http;
    private readonly AiOptions _options;
    private readonly ILogger<OllamaAiAssistant> _logger;

    public OllamaAiAssistant(
        HttpClient http, IOptions<AiOptions> options, ILogger<OllamaAiAssistant> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
        _http.BaseAddress = new Uri(_options.Ollama.BaseUrl.TrimEnd('/') + "/");
        _http.Timeout = TimeSpan.FromSeconds(Math.Max(10, _options.TimeoutSeconds));
    }

    public bool IsEnabled => true;
    public string Description => $"ollama/{_options.Ollama.Model}";

    public async Task<string?> CompleteAsync(
        string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        try
        {
            var request = new ChatRequest(
                _options.Ollama.Model,
                [new("system", systemPrompt), new("user", userPrompt)],
                Stream: false,
                // Low temperature = less "creative", more consistent. Right for
                // maintenance text where we want the same facts → same answer.
                Options: new ChatOptions(Temperature: 0.2, NumPredict: 300));

            using var response = await _http.PostAsJsonAsync("api/chat", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Ollama returned {Status}: {Body}", (int)response.StatusCode, body[..Math.Min(200, body.Length)]);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken);
            return result?.Message?.Content;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Ollama timed out after {Seconds}s (model still loading?)", _options.TimeoutSeconds);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama call failed");
            return null;
        }
    }

    // ── wire types (Ollama /api/chat) ────────────────────────────────────────
    private sealed record ChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] List<ChatMessage> Messages,
        [property: JsonPropertyName("stream")] bool Stream,
        [property: JsonPropertyName("options")] ChatOptions Options);

    private sealed record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ChatOptions(
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("num_predict")] int NumPredict);

    private sealed class ChatResponse
    {
        [JsonPropertyName("message")] public ChatMessage? Message { get; set; }
        [JsonPropertyName("done")] public bool Done { get; set; }
    }
}
