using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TtsWebApp.Services;

public record TranslationResult(string Language, string LanguageCode, string Text);

public class TranslationService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public static readonly (string Name, string Code, string Instruction)[] TargetLanguages =
    {
        ("Korean", "ko", "Korean"),
        ("English", "en", "English"),
        ("German", "de", "German"),
    };
    
    public TranslationService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public async Task<List<TranslationResult>> TranslateAllAsync(
        string text,
        CancellationToken ct = default)
    {
        var apiKey = _config["OpenAi:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException(
                "OpenAI API key is not configured. " +
                "Set it with 'dotnet user-secrets set \"OpenAi:ApiKey\" \"YOUR_API_KEY\"'");
        
        var systemPrompt = 
            "You are a professional translator. " +
            "The user will provide text text in any language. " +
            "Translate it into Korean, English, and German. " +
            "Return ONLY a JSON object with exactly three keys: 'ko', 'en', and 'de'. " +
            "Each key's value should be the translated text in that language. " +
            "If the source text is already in one of those languages, return it verbatim for that key. " +
            "No explanation, no markdown, only the JSON object.";
        
        var body = new
        {
            model = "gpt-4o-mini",
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = text }
            },
            temperature = 0.2,
            response_format = new { type = "json_object" }
        };
 
        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");
 
        var response = await _http.SendAsync(request, ct);
 
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"OpenAI Chat API call failed ({(int)response.StatusCode}): {detail}");
        }
            
            var json = await response.Content.ReadAsStringAsync(ct);

            using var doc = JsonDocument.Parse(json);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "{}";
            
            using var inner = JsonDocument.Parse(content);
            var root = inner.RootElement;
            
            var result = new List<TranslationResult>();
            foreach (var (name, code, _) in TargetLanguages)
            {
                var translated = root.TryGetProperty(code, out var val)
                    ? val.GetString() ?? ""
                    : "";
                result.Add(new TranslationResult(name, code, translated));

            }
        
        return result;
    }
}
