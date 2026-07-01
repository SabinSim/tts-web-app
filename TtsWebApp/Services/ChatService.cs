using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TtsWebApp.Services;
    
public record ChatMessage(string Role, string Content);

public class ChatService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public static readonly (string Code, string Label, string NativeName)[] Languages =
    {
        ("de", "Deutsch", "German"),
        ("en", "English", "English"),
        ("es", "힌국어", "Korean"),
    };

    public static readonly (string Code, string Label, string Description)[] Categories =
    {
        ("daily",     "Daily",     "everyday casual conversation"),
        ("business",  "Business",  "professional and workplace communication"),
        ("travel",    "Travel",    "travel, directions, hotels, and restaurants"),
        ("interview", "Interview", "job interviews and self-introduction"),
    };

    public static readonly (string Code, string Label, string Instruction)[] Levels =
    {
        ("beginner", "Beginner", "Basic vocabulary and simple senetences"),
        ("intermediate", "Intermediate", "More complex setences and grammar"),
        ("advanced", "Advanced", "Use of idioms, phrasal verbs, and nuanced expressions")
    };
    
    public ChatService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public async Task<string> SendMessageAsync(
        List<ChatMessage> history,
        string languageCode,
        string categoryCode,
        string levelCode,
        CancellationToken ct = default)
    {
        var apiKey = _config["OpenAi:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI API key is not configured.");
        
        var lang     = Languages.First(l => l.Code == languageCode);
        var category = Categories.First(c => c.Code == categoryCode);
        var level    = Levels.First(l => l.Code == levelCode);

        var systemPrompt =
            $"You are a friendly language converstaion partner helping the user practice {lang.NativeName}." +
            $"The topic is : {category.Description}. " +
            $"Language level: {level.Label}. {level.Instruction}. " +
            $"Keep your replies conversational and concise (2-4 sentences). " +
            $"Gently correct any grammar mistakes the user makes by including the correction naturally in your reply." +
            $"Start the conversation with a warm greeting and an opening question related to the topic.";

        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt }
        };
        
        foreach (var msg in history)
            messages.Add(new {role = msg.Role, content = msg.Content});

        var body = new
        {
            model = "gpt-4o-mini",
            messages,
            temperature = 0.8,
            humidity = 0.8,
            max_tokens = 300
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(body);
        
        var response = await _http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"OpenAi Chat API call failed ({(int)response.StatusCode}): {detail}");
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
