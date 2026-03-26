# Repository Agents Guide

This repository is a multi-client workspace for the OpenAI gateway service.

## Project goals

- Keep one contract-first model for all clients using `openapi.yaml`.
- Keep parity across web (TS), web (.NET), and desktop/mobile (.NET MAUI) behavior.
- Avoid duplicating transport, auth, retry, streaming, and error handling logic.
- Keep docs, prompts, custom agents, and tests in sync with behavior changes.

## Working agreement

- Read `README.md`, `openapi.yaml`, and `docs/ai-workflow.md` before broad changes.
- Treat this repo as security-sensitive: never commit secrets, tokens, local keys, or private certificates.
- Prefer minimal diffs and preserve existing conventions per project folder.
- When changing API-facing behavior, update:
  - implementation
  - tests
  - `openapi.yaml` sync or generation notes
  - relevant docs and prompt files
- Keep generated artifacts and local-only overrides out of version control.

## Commands

- Web install: `npm --prefix web install`
- Web dev: `npm --prefix web run dev`
- Web check: `npm --prefix web run check`
- Web lint: `npm --prefix web run lint`
- Web format check: `npm --prefix web run format`
- Web test: `npm --prefix web test`
- Dotnet restore: `dotnet restore OpenAiServiceClients.slnx`
- Dotnet build core: `dotnet build dotnet/src/Core/OpenAiServiceClients.Core.csproj`
- Dotnet build web: `dotnet build dotnet/src/Web/OpenAiServiceClients.Web.csproj`
- Dotnet build maui: `dotnet build dotnet/src/Maui/OpenAiServiceClients.Maui.csproj`
- Dotnet test: `dotnet test dotnet/tests/Core.Tests/OpenAiServiceClients.Core.Tests.csproj`
- MAUI workloads: `dotnet workload restore`

## Maintenance cadence

- Run a lightweight maintenance pass monthly.
- Run a deeper architecture and tooling review quarterly.
- Use `docs/maintenance-cadence.md` as the checklist.
- Treat OpenAPI drift and platform SDK updates as normal maintenance.

## Done criteria

- Relevant web and/or dotnet checks pass.
- `npm --prefix web run lint` passes for web changes.
- `npm --prefix web run format` passes for web changes.
- Contract sync implications are handled.
- Tests are updated for behavioral changes.
- Docs, prompts, and agents are updated when workflow behavior changes.
