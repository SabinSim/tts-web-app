using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace TtsWebApp.Services;

/// <summary>
/// OpenAI TTS API를 호출해 텍스트를 음성(mp3)으로 변환하는 서비스
/// </summary>
public class OpenAiTtsService
{
    // ==================== 1. 의존성 주입 ====================
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public OpenAiTtsService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    // ==================== 2. 사용 가능한 음성 목록 ====================
    public static readonly string[] AvailableVoices =
    {
        "alloy", "ash", "coral", "echo", "fable", "nova", "onyx", "sage", "shimmer"
    };

    public static readonly string[] AvailableModels =
        { "tts-1", "tts-1-hd" };

    // ==================== 3. 음성 합성 메서드 ====================
    public async Task<byte[]> SynthesizeSpeechAsync(
        string text, 
        string? voice = null, 
        string? model = null,
        float speed = 1.0f,
        CancellationToken ct = default)
    {
        // 3-1. API 키 검증
        var apiKey = _config["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "OpenAI API key is not configured. Please set the 'OpenAI:ApiKey' in your configuration." +
                "You can set it in local 'dotnet user-secrets set \"OpenAI:ApiKey\" \"sk-...\"' or in your environment variables." +
                "You can get your API key from https://platform.openai.com/account/api-keys");
        }

        // 3-2. 모델 및 음성 설정
        var resolvedModel = string.IsNullOrWhiteSpace(model)
            ? (_config["OpenAI:Model"] ?? "tts-1")
            : model;
 
        var resolvedVoice = string.IsNullOrWhiteSpace(voice)
            ? (_config["OpenAI:Voice"] ?? "alloy")
            : voice;

        var clampedSpeed = Math.Clamp(speed, 0.25f, 4.0f);
        

        // 3-3. HTTP 요청 구성
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

        // 3-4. API 호출
        var response = await _http.SendAsync(request, ct);

        // 3-5. 응답 처리 및 에러 핸들링
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"OpenAI TTS API call failed ({(int)response.StatusCode}): {detail}");
        }

        // 3-6. 음성 데이터 반환
        return await response.Content.ReadAsByteArrayAsync(ct);
    }
}
