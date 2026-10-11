using System.Text;
using System.Text.Json;

namespace ManagerCompass.Api.Services;

/// <summary>
/// OpenAI Chat Completions provider. Key comes from the OPENAI_API_KEY environment variable (.env or user-secret);
/// model from Ai:OpenAiModel (default gpt-4o-mini).
/// </summary>
public class OpenAiClient : IAiClient
{
    private readonly HttpClient _http;
    private readonly string? _apiKey;
    private readonly string _model;

    public OpenAiClient(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["OPENAI_API_KEY"];
        _model = config["Ai:OpenAiModel"] ?? "gpt-4o-mini";
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userContent, int maxTokens, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_apiKey)) throw new AiNotConfiguredException();

        var body = JsonSerializer.Serialize(new
        {
            model = _model,
            max_tokens = maxTokens,
            temperature = 0.3,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userContent }
            }
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {_apiKey}");

        using var response = await _http.SendAsync(request, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"OpenAI API returned {(int)response.StatusCode}: {json}");

        using var doc = JsonDocument.Parse(json);
        var text = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        return text ?? throw new HttpRequestException("OpenAI API returned no text.");
    }
}
