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

1. Install web dependencies with `npm --prefix web install`.
2. Restore the .NET solution with `dotnet restore OpenAiServiceClients.slnx`.
3. Restore MAUI workloads with `dotnet workload restore` before building the MAUI app.
4. Sync `openapi.yaml` from the gateway repo with `scripts/sync-openapi.ps1` or `scripts/sync-openapi.sh` when the API contract changes.

## Current status

- `web/` contains a working Vite + React + TypeScript starter app.
- `dotnet/src/Core/` contains the shared .NET core project.
- `dotnet/src/Web/` contains a scaffolded ASP.NET Core app.
- `dotnet/src/Maui/` contains a MAUI project skeleton that requires local MAUI workloads.
- `dotnet/tests/Core.Tests/` contains the shared .NET test project.

## Validation commands

- Web install: `npm --prefix web install`
- Web check: `npm --prefix web run check`
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
