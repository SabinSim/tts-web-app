using TtsWebApp.Components;
using TtsWebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Added: fixes a conflict with internal ASP.NET Core services caused by IConfiguration being registered as Scoped in .NET 10
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

// Added: register the translation service (OpenAI Chat API, gpt-4o-mini)
builder.Services.AddHttpClient<TranslationService>(client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Added: register the AI chat service (language learning chatbot)
builder.Services.AddHttpClient<ChatService>(client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<WhisperService>(client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/");
    client.Timeout = TimeSpan.FromSeconds(60);
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
app.MapPost("/api/tts", async (TtsRequest req, OpenAiTtsService tts) =>
{
    if (string.IsNullOrWhiteSpace(req.Text))
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