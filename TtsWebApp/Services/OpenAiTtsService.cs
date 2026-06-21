using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace TtsWebApp.Services;

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
        var apiKey = _config["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "OpenAI API 키가 설정되지 않았습니다. " +
                "로컬에서는 'dotnet user-secrets set \"OpenAI:ApiKey\" \"sk-...\"' 로 설정하고, " +
                "배포 환경에서는 환경 변수 OpenAI__ApiKey 로 설정하세요.");
        }

        var model = _config["OpenAI:Model"] ?? "tts-1";
        var voice = _config["OpenAI:Voice"] ?? "alloy";

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/audio/speech");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(new
        {
            model,
            voice,
            input = text,
            response_format = "mp3"
        });

        var response = await _http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"OpenAI TTS 호출 실패 ({(int)response.StatusCode}): {detail}");
        }

        return await response.Content.ReadAsByteArrayAsync(ct);
    }
}