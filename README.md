# OpenAI Service Clients

Monorepo for client applications targeting the `openai-api-service` gateway.

## Scope

This repo hosts three client surfaces:

- `web/`: TypeScript Node.js web app for Vercel
- `dotnet/src/Web/`: .NET web app
- `dotnet/src/Maui/`: .NET MAUI app

All client surfaces should share one API contract and one core .NET client library where possible.

## Repository layout

- `openapi.yaml`: API contract source used to generate/update typed clients
- `web/`: TypeScript web client
- `dotnet/src/Core/`: shared .NET API client and DTOs
- `dotnet/src/Web/`: ASP.NET Core web client
- `dotnet/src/Maui/`: .NET MAUI client
- `dotnet/tests/Core.Tests/`: tests for the shared .NET client layer
- `docs/`: workflow and maintenance docs
- `.github/`: Copilot instructions, prompts, and custom agents

## First-time setup

0. Install .NET 10 SDK and verify with `dotnet --version`.
1. Install web dependencies with `npm --prefix web install`.
2. Restore the .NET solution with `dotnet restore OpenAiServiceClients.slnx`.
3. Restore MAUI workloads with `dotnet workload restore` before building the MAUI app.
4. Sync `openapi.yaml` from the gateway repo with `scripts/sync-openapi.ps1` or `scripts/sync-openapi.sh` when the API contract changes.

## Current status

### Gateway endpoint coverage

| Gateway endpoint       | web (TS) | .NET Core | .NET Web | MAUI |
|------------------------|:--------:|:---------:|:--------:|:----:|
| `/health`              | ✅       | ✅        | ✅       | ✅   |
| `/v1/llm` (non-stream) | ✅      | ✅        | ✅       | ✅   |
| `/v1/llm` (stream)    | ✅       | ✅        | ✅       | ✅   |
| `/v1/whisper`          | ✅       | ✅        | ✅       | ✅   |
| `/v1/usage`            | ✅       | ✅        | ✅       | —    |
| `/v1/admin/usage`      | ✅       | ✅        | ✅       | —    |
| `/v1/models`           | —        | ✅        | —        | ✅   |
| `/v1/admin/retention`  | —        | —         | —        | —    |

### Client details

- **`web/`** — Vite + React + TypeScript SPA with Vitest unit tests. Covers health, LLM (non-stream + stream with SSE parsing), Whisper upload, usage, and admin usage.
- **`dotnet/src/Core/`** — Shared .NET gateway client (`GatewayClient`) with JSON, SSE stream, multipart, and query-string methods. Includes `LlmPayloadHelper` for usage extraction and `ModelQueryPolicy` for model availability checks.
- **`dotnet/src/Web/`** — ASP.NET Core minimal API with `/api/health`, `/api/llm`, `/api/llm/stream`, `/api/whisper`, `/api/usage`, and `/api/admin/usage` routes plus a static HTML UI.
- **`dotnet/src/Maui/`** — .NET MAUI app with health, LLM (non-stream + stream), Whisper (audio file picker), server-driven model picker, usage stats display, and a Settings page for API keys, whisper model, pagination, and admin auto-lock.
- **`dotnet/tests/Core.Tests/`** — xUnit tests for the shared .NET client layer including streaming and payload helper coverage.

## Validation commands

- Web install: `npm --prefix web install`
- Web check: `npm --prefix web run check`
- Web test: `npm --prefix web test`
- Web build: `npm --prefix web run build`
- Dotnet restore: `dotnet restore OpenAiServiceClients.slnx`
- Dotnet build (Core): `dotnet build dotnet/src/Core/OpenAiServiceClients.Core.csproj`
- Dotnet build (Web): `dotnet build dotnet/src/Web/OpenAiServiceClients.Web.csproj`
- Dotnet test: `dotnet test dotnet/tests/Core.Tests/OpenAiServiceClients.Core.Tests.csproj`
- MAUI build: `dotnet build dotnet/src/Maui/OpenAiServiceClients.Maui.csproj`

## Deployment notes

- Vercel should point at `web/` as the project root directory.
- The `.NET` web app should be deployed separately from the Vercel app.
- The MAUI app is a client build target, not a hosted deployment target.

## Workflow

Use `AGENTS.md` and `docs/ai-workflow.md` as the primary operating guide.
