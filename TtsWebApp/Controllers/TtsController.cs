using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace TtsWebApp.Controllers;

/// <summary>
/// A service that calls the OpenAI TTS API (/v1/audio/speech) to convert text into speech (mp3).
/// </summary>

public class OpenAiTtsService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public OpenAiTtsService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public async Task<byte[]> SynthesizeSpeechAsync(string text, CancellationToken ct = default)
    {
        // ==================== 1. Validate configuration ====================
        var apiKey = _config["OpenAi:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "OpenAI API Key is not configured. Please set the 'OpenAi:ApiKey' in your configuration." + 
                "You can set it in local 'dotnet user-secrets set \"OpenAI:ApiKey\" \"sk-...\"' . " + 
                "You can get your API key from https://platform.openai.com/account/api-keys");
        }

        // ==================== 2. Configure API parameters ====================
        var model = _config["OpenAi:Model"] ?? "tts-1";
        var voice = _config["OpenAi:Voice"] ?? "alloy";
        
        // ==================== 3. Build HTTP request ====================
        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/audio/speech");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",  apiKey);
        request.Content = JsonContent.Create(new
        {
            model,
            voice,
            input = text,
            response_format = "mp3"
        });
        
        // ==================== 4. Call the API ====================
        var response = await _http.SendAsync(request, ct);

        // ==================== 5. Handle response ====================
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"OpenAI TTS API request failed with status code ({(int)response.StatusCode}): {detail}");
        }

        // ==================== 6. Return the result ====================
        return await response.Content.ReadAsByteArrayAsync(ct);
    }
}
