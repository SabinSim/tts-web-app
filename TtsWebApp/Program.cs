using TtsWebApp.Components;
using TtsWebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// 추가됨: .NET 10에서 IConfiguration이 Scoped로 등록되면서 ASP.NET Core 내부 서비스와 충돌하는 문제 해결
builder.Services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(builder.Configuration);

// Razor components (Blazor Server, interactive render mode)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// HttpClient + service registration for calling the OpenAI TTS API
builder.Services.AddHttpClient<OpenAiTtsService>(client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// 추가됨: 번역 서비스 등록 (OpenAI Chat API, gpt-4o-mini)
builder.Services.AddHttpClient<TranslationService>(client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// 추가됨: 채팅 서비스 등록 (OpenAI Chat API, gpt-4o-mini)
builder.Services.AddHttpClient<ChatService>(client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// TTS API endpoint that can also be called externally (useful for testing/debugging)
// MODIFIED: comment translated from Korean to English
app.MapPost("/api/tts", async (TtsRequest req, OpenAiTtsService tts) =>
{
    if (string.IsNullOrWhiteSpace(req.Text))
        // MODIFIED: error message translated from Korean ("text 필드가 필요합니다.") to English
        return Results.BadRequest(new { error = "The 'text' field is required." });

    try
    {
        var audio = await tts.SynthesizeSpeechAsync(req.Text);
        return Results.Bytes(audio, "audio/mpeg");
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.Run();

record TtsRequest(string Text);