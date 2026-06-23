# TTS Web App

A web app that converts typed text into natural-sounding speech, built with ASP.NET Core
and Blazor Server, using the OpenAI Text-to-Speech API.

## Live demo

http://tts-web-app-sabin.azurewebsites.net

(Hosted on Azure App Service, Free tier — the app may take a few seconds to wake up after
a period of inactivity.)

## Features

- Convert text to speech in any language — English, German, Korean, etc. The model
  detects the language automatically from the input text.
- Choose between 9 different voices (alloy, ash, coral, echo, fable, nova, onyx, sage, shimmer).
- Live character counter with a 4,096-character limit warning.
- Conversion history for the current session — replay or delete any past result.
- Clean, responsive card-based UI.

## Tech stack

- **Backend:** ASP.NET Core 10 (Blazor Server, interactive render mode)
- **Speech synthesis:** OpenAI TTS API (`tts-1` / `tts-1-hd`)
- **Hosting:** Azure App Service (Linux, Free tier)
- **Language:** C#

## Getting started

1. Make sure the .NET SDK is installed:
   ```
   dotnet --version
   ```
2. Get an API key from [platform.openai.com](https://platform.openai.com/api-keys) and
   store it with User Secrets (never commit a real key to source control):
   ```
   cd TtsWebApp
   dotnet user-secrets init
   dotnet user-secrets set "OpenAI:ApiKey" "sk-..."
   ```
3. Run the app:
   ```
   dotnet run
   ```
4. Open the printed `https://localhost:****` URL, type some text, pick a voice, and click
   **Generate speech**.

## Project structure

```
TtsWebApp/                              ← solution root
├─ TtsWebApp.sln
├─ global.json
├─ .gitignore
├─ README.md
└─ TtsWebApp/                           ← project folder
   ├─ TtsWebApp.csproj
   ├─ Program.cs                        ← app entry point, registers Blazor + the /api/tts endpoint
   ├─ appsettings.json                  ← OpenAI:ApiKey (kept empty), OpenAI:Model, OpenAI:Voice
   ├─ Services/
   │  └─ OpenAiTtsService.cs            ← calls the OpenAI /v1/audio/speech endpoint
   └─ Components/
      ├─ App.razor
      ├─ Routes.razor
      ├─ Layout/
      └─ Pages/
         ├─ Home.razor                  ← main UI: text input, voice picker, history
         └─ Home.razor.css
```

## Deployment

Deployed to Azure App Service with the Azure CLI:

```
az webapp up --name tts-web-app-sabin --resource-group tts-web-app-rg \
  --os-type linux --runtime "DOTNETCORE|10.0" --sku F1
```

The API key is stored as an App Service environment variable (not in source control):

```
az webapp config appsettings set --name tts-web-app-sabin \
  --resource-group tts-web-app-rg --settings OpenAI__ApiKey="sk-..."
```

## Notes

- **API key handling:** locally via `dotnet user-secrets`, in production via the
  `OpenAI__ApiKey` environment variable. Never hardcode a key in `appsettings.json`.
- **Pricing:** OpenAI's `tts-1` model costs $15 per 1M characters (`tts-1-hd` is $30 per 1M)
  — see [OpenAI's pricing page](https://openai.com/api/pricing/) for current rates.
- **History** is in-memory only and resets on page refresh or app restart.

## Possible next steps

- Persist conversion history (local storage or a database)
- Add automated tests
- Set up CI/CD (GitHub Actions → Azure) for one-step deploys on push
