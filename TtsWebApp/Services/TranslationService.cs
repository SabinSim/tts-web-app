using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TtsWebApp.Services;

public record TranslationResult(string Language, string LanguageCode, string Text);

public class TranslationService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public (string Code, string Label, string Instruction)[] Languages =>
        new[]
        {
            ("auto", "Auto Detect", ""),
            ("ko", "Korean", "Korean"),
            ("en", "English", "English"),
            ("de", "German", "German"),
        };
    
    public TranslationService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public async Task<string> TranslateAsync(
        string text,
        string sourceCode,
        string targetCode,
        CancellationToken ct = default)
    {
        var apiKey = _config["OpenAi:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException(
                "OpenAI API key is not configured. " +
                "Set it with 'dotnet user-secrets set \"OpenAi:ApiKey\" \"YOUR_API_KEY\"'");
        
        var targetLabel = this.Languages.First(l => l.Code == targetCode).Instruction;
        
        var sourceHint = sourceCode == "auto"
            ? "Detect the source language automatically."
            : $"The source language is {this.Languages.First(l => l.Code == sourceCode).Instruction}.";

        var systemPrompt =
            $"You are a professional translator. {sourceHint}" +
            $"Translate the user's text into {targetLabel}" +
            "Return ONLY the translated text - no explation, no quotes, no markdown.";
        
        var body = new
        {
            model = "gpt-4o-mini",
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = text }
            },
            temperature = 0.2
        };
 
        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(body);
        
        var response = await _http.SendAsync(request, ct);
 
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"OpenAI Chat API call failed ({(int)response.StatusCode}): {detail}");
        }
            
        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
 
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";
    }
}
