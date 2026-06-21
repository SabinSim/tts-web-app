using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace TtsWebApp.Controllers;

/// <summary>
/// OpenAI TTS API(/v1/audio/speech)를 호출해 텍스트를 음성(mp3)으로 변환하는 서비스.
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
        // ==================== 1. 설정 검증 ====================
        var apiKey = _config["OpenAi:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "OpenAI API Key is not configured. Please set the 'OpenAi:ApiKey' in your configuration." + 
                "You can set it in local 'dotnet user-secrets set \"OpenAI:ApiKey\" \"sk-...\"' . " + 
                "You can get your API key from https://platform.openai.com/account/api-keys");
        }

        // ==================== 2. API 파라미터 설정 ====================
        var model = _config["OpenAi:Model"] ?? "tts-1";
        var voice = _config["OpenAi:Voice"] ?? "alloy";
        
        // ==================== 3. HTTP 요청 구성 ====================
        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/audio/speech");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",  apiKey);
        request.Content = JsonContent.Create(new
        {
            model,
            voice,
            input = text,
            response_format = "mp3"
        });
        
        // ==================== 4. API 호출 ====================
        var response = await _http.SendAsync(request, ct);

        // ==================== 5. 응답 처리 ====================
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"OpenAI TTS API request failed with status code ({(int)response.StatusCode}): {detail}");
        }

        // ==================== 6. 결과 반환 ====================
        return await response.Content.ReadAsByteArrayAsync(ct);
    }
}
