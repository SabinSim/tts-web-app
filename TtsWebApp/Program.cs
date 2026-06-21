using TtsWebApp.Components;
using TtsWebApp.Services;

// ==================== 1. 빌더 생성 ====================

var builder = WebApplication.CreateBuilder(args);

// ==================== 2. 서비스 등록 (DI 컨테이너) ====================

// Add services to the container.
// 아래 코드는 Razor Components와 Interactive Server Components를 추가하는 부분입니다.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 아래 코드는 OpenAI TTS 서비스를 DI 컨테이너에 등록하는 부분입니다.
// The code below registers the OpenAI TTS service in the DI container.
builder.Services.AddHttpClient<OpenAiTtsService>(client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});


// ==================== 3. 애플리케이션 빌드 ====================

// 아래 코드는 OpenAI TTS 서비스를 싱글톤으로 등록하는 부분입니다. 쉽게 말해 애플리케이션 전체에서 하나의 인스턴스만 사용하도록 설정하는 것입니다.
// 인스턴트란 클래스의 객체를 의미하며, 싱글톤으로 등록하면 애플리케이션 전체에서 하나의 인스턴스만 생성되어 사용됩니다. 이는 리소스 관리와 성능 향상에 도움이 될 수 있습니다.
// The code below registers the OpenAI TTS service as a singleton.
var app = builder.Build();

// ==================== 4. 개발/프로덕션 환경 설정 ====================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// ==================== 5. 보안 & 미들웨어 설정 ====================

// 아래 코드는 HTTPS 리디렉션, 정적 파일 제공, CSRF 방지 등을 설정하는 부분입니다. 쉽게 말해, 보안과 관련된 설정을 하는 것입니다.
// The code below sets up HTTPS redirection, static file serving, and CSRF protection.
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// ==================== 6. UI 라우팅 설정 ====================
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// ==================== 7. API 엔드포인트 정의 ====================
// API 엔드포인트를 정의하는 이유는 클라이언트가 서버에 요청을 보내고, 서버가 그 요청을 처리하여 응답을 반환할 수 있도록 하기 위함입니다.
// API 엔드포인트는 클라이언트와 서버 간의 통신 경로를 정의하는 역할을 합니다.
app.MapPost("/api/tts", async (TtsRequest req, OpenAiTtsService tts) =>
{
    if (string.IsNullOrWhiteSpace(req.Text))
        return Results.BadRequest(new { error = "Text is required." });

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

