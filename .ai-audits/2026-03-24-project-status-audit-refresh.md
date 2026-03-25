# Project Status Audit

- Date: 2026-03-24
- Reviewer: Codex (GPT-5)
- Scope: `openai-service-clients` current worktree, including uncommitted changes

## Summary

The repository is now well beyond starter state. The shared .NET Core client covers health, JSON LLM, streaming LLM, whisper upload, usage, and admin usage, and those capabilities are now surfaced in the TypeScript web app, the ASP.NET Core adapter, and the MAUI app. The main remaining gaps are verification quality and the last pieces of contract coverage.

## Commands run

- `npm --prefix web run check`
- `npm --prefix web run build`
- `npm --prefix web test`
- `dotnet restore OpenAiServiceClients.slnx`
- `dotnet build dotnet/src/Core/OpenAiServiceClients.Core.csproj --no-restore`
- `dotnet build dotnet/src/Web/OpenAiServiceClients.Web.csproj --no-restore`
- `dotnet test dotnet/tests/Core.Tests/OpenAiServiceClients.Core.Tests.csproj --no-restore`
- `dotnet build dotnet/src/Maui/OpenAiServiceClients.Maui.csproj --no-restore`
- `dotnet build dotnet/src/Maui/OpenAiServiceClients.Maui.csproj -f net9.0-windows10.0.19041.0 --no-restore`
- `dotnet build dotnet/src/Maui/OpenAiServiceClients.Maui.csproj -f net9.0-android --no-restore`

## Findings

### High

- Web CI is still red by default. `web/package.json` still defines `vitest run`, the `web` workflow still executes `npm --prefix web test`, and there are still no matching web test files. `npm --prefix web test` fails with "No test files found".

### Medium

- Contract coverage is now broad but still partial. The implemented surfaces cover health, JSON LLM, streaming LLM, whisper file upload, usage, and admin usage, but the contract still includes `/v1/admin/retention` plus whisper `audio_url` and whisper streaming options that are not represented in the current client code.
- Automated coverage is concentrated in the shared Core layer. Core now has 11 passing tests covering health, JSON LLM, streaming LLM, whisper upload, usage, and admin usage, but there are still no web tests and no route or UI tests for the ASP.NET Core or MAUI surfaces.
- MAUI is in a materially better state, but the full multi-target build is still not clean on this host. Targeted Windows and Android builds pass, while the full MAUI build still fails here on iOS and MacCatalyst cleanup with `MSB3231` access-denied errors. The current .NET workflow does not validate MAUI, so that gap would not be caught in CI.

### Low

- `README.md` still understates the current implementation. The code now includes whisper, usage, and admin usage flows in addition to health and LLM support, but the status section only describes the smaller earlier subset.
- The maintenance workflow is now in good shape and is being used. `docs/maintenance-cadence.md`, `docs/maintenance-reviews/README.md`, and the dated reports in `docs/maintenance-reviews/` now form a coherent maintenance trail.

## Verification

- Passed: `npm --prefix web run check`
- Passed: `npm --prefix web run build`
- Failed: `npm --prefix web test`
- Passed: `dotnet restore OpenAiServiceClients.slnx`
- Passed: `dotnet build` for Core
- Passed: `dotnet build` for Web
- Passed: `dotnet test` for Core.Tests with 11 passing tests
- Failed: full multi-target `dotnet build` for MAUI on this Windows host due to iOS and MacCatalyst cleanup/access issues
- Passed: targeted MAUI Windows build
- Passed: targeted MAUI Android build

## Limitations

- This audit was local-only. It did not re-verify external OpenAI, Vercel, or MAUI guidance against current online documentation.
- This audit used the dirty worktree as the source of truth, not only committed files.
