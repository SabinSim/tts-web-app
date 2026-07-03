using System.Net.Http.Headers;
using System.Text.Json;

namespace TtsWebApp.Services;

public class WhisperService
{
    private readonly HttpClient _http;
    public readonly IConfiguration _config;
    
    public WhisperService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public async Task<string> TranscribeAsync(
        byte[] audioBytes,
        string? languageCode = null,
        CancellationToken ct = default)
    {
        var apiKey = _config["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI API key is not configured.");
        
        using var form = new MultipartFormDataContent();
        
        var audioContent = new ByteArrayContent(audioBytes);
        audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/webm");
        form.Add(audioContent, "file", "recording.webm");
        form.Add(new StringContent("whisper-1"), "model");
        
        if (!string.IsNullOrWhiteSpace(languageCode))
            form.Add(new StringContent(languageCode), "language");

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/audio/transcriptions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = form;
        
        var response = await _http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Whisper API call failed ({(int)response.StatusCode}): {detail}");
        }
        
        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        return doc.RootElement
            .GetProperty("text")
            .GetString() ?? "";

    }
}