using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ManagerCompass.Api.Services;

public class AiNotConfiguredException : Exception
{
    public AiNotConfiguredException() : base("No AI API key (ANTHROPIC_API_KEY or OPENAI_API_KEY) is set on the API server.") { }
}

public interface IAiClient
{
    Task<string> CompleteAsync(string systemPrompt, string userContent, int maxTokens, CancellationToken ct);
}

/// <summary>
/// Thin wrapper over the Anthropic Messages API. The key comes from configuration — the ANTHROPIC_API_KEY
/// environment variable (or a user-secret of the same name) — never from appsettings.json or the browser.
/// </summary>
public class ClaudeClient : IAiClient
{
    private readonly HttpClient _http;
    private readonly string? _apiKey;
    private readonly string _model;

    public ClaudeClient(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["ANTHROPIC_API_KEY"];
        _model = config["Ai:Model"] ?? "claude-sonnet-5-5";
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userContent, int maxTokens, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_apiKey)) throw new AiNotConfiguredException();

        var body = JsonSerializer.Serialize(new
        {
            model = _model,
            max_tokens = maxTokens,
            system = systemPrompt,
            messages = new[] { new { role = "user", content = userContent } }
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = new StringContent(body, Encoding.UTF8, new MediaTypeHeaderValue("application/json"))
        };
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        using var response = await _http.SendAsync(request, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Anthropic API returned {(int)response.StatusCode}: {json}");

        using var doc = JsonDocument.Parse(json);
        var text = doc.RootElement.GetProperty("content")
            .EnumerateArray()
            .Where(b => b.GetProperty("type").GetString() == "text")
            .Select(b => b.GetProperty("text").GetString())
            .FirstOrDefault();
        return text ?? throw new HttpRequestException("Anthropic API returned no text.");
    }
}
