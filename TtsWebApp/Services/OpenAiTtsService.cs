using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace TtsWebApp.Services;

/// <summary>
/// A service that calls the OpenAI TTS API to convert text into speech (mp3)
/// </summary>
public class OpenAiTtsService
{
    // ==================== 1. Dependency injection ====================
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public OpenAiTtsService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    // ==================== 2. List of available voices ====================
    public static readonly string[] AvailableVoices =
    {
        "alloy", "ash", "coral", "echo", "fable", "nova", "onyx", "sage", "shimmer"
    };

    public static readonly string[] AvailableModels =
        { "tts-1", "tts-1-hd" };

    // ==================== 3. Speech synthesis method ====================
    public async Task<byte[]> SynthesizeSpeechAsync(
        string text, 
        string? voice = null, 
        string? model = null,
        float speed = 1.0f,
        CancellationToken ct = default)
    {
        // 3-1. Validate API key
        var apiKey = _config["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "OpenAI API key is not configured. Please set the 'OpenAI:ApiKey' in your configuration." +
                "You can set it in local 'dotnet user-secrets set \"OpenAI:ApiKey\" \"sk-...\"' or in your environment variables." +
                "You can get your API key from https://platform.openai.com/account/api-keys");
        }

        // 3-2. Configure model and voice
        var resolvedModel = string.IsNullOrWhiteSpace(model)
            ? (_config["OpenAI:Model"] ?? "tts-1")
            : model;
 
        var resolvedVoice = string.IsNullOrWhiteSpace(voice)
            ? (_config["OpenAI:Voice"] ?? "alloy")
            : voice;

        var clampedSpeed = Math.Clamp(speed, 0.25f, 4.0f);
        

        // 3-3. Build HTTP request
        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/audio/speech");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(new
        {
            model = resolvedModel,
            voice = resolvedVoice,
            input = text,
            response_format = "mp3",
            speed = clampedSpeed
        });

        // 3-4. Call the API
        var response = await _http.SendAsync(request, ct);

        // 3-5. Handle response and errors
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"OpenAI TTS API call failed ({(int)response.StatusCode}): {detail}");
        }

        // 3-6. Return the audio data
        return await response.Content.ReadAsByteArrayAsync(ct);
    }
}
