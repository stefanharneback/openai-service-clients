# Project Status Audit Refresh 3

Date: 2026-03-23
Repository: `openai-service-clients`
Scope assumption: This audit treats the current uncommitted worktree as the source of truth because the repo is dirty and the user asked for the latest project status.

## Findings

### High

1. The web validation path is still red because the configured test step has no matching test files.
   Evidence:
   - `web/package.json:11` defines `vitest run`.
   - `.github/workflows/web.yml:24-31` always runs the web test step for web changes.
   - `npm --prefix web test` still fails with `No test files found`.
   Risk:
   - Any web PR is likely to fail CI even when the implementation is otherwise valid.
   - The newly added streaming UI path in the TypeScript client has no regression protection.
   Suggested fix:
   - Add at least one web test for `/health` and one for the new streaming behavior, or explicitly configure a temporary zero-test policy.

### Medium

2. Streaming support is now implemented across the repo, but only the shared Core helper has automated coverage.
   Evidence:
   - `README.md:35-39` now claims stream support in `web`, `.NET Core`, `.NET Web`, and MAUI.
   - `web/src/api/client.ts:49-144` implements SSE parsing for the browser client.
   - `dotnet/src/Web/Program.cs:60-101` adds `/api/llm/stream`.
   - `dotnet/src/Maui/MainPage.xaml.cs:74-169` adds streaming UI/event handling and chunk parsing.
   - `dotnet/tests/Core.Tests/GatewayClientTests.cs:94-149` covers only the Core streaming helper.
   Risk:
   - The repo now advertises streaming broadly, but there are still no route-level, UI-level, or end-to-end tests for the web, `.NET Web`, or MAUI stream paths.
   Suggested fix:
   - Add focused tests for the web SSE parser, the `.NET Web` stream proxy route, and at least one app-surface smoke test that exercises streaming behavior.

3. Full MAUI multi-target build is still not clean in this environment, and CI still does not validate MAUI at all.
   Evidence:
   - `dotnet build dotnet/src/Maui/OpenAiServiceClients.Maui.csproj --no-restore` still fails on iOS/MacCatalyst cleanup with `MSB3231` access-denied errors under `obj\Debug\...`.
   - Targeted builds now pass for:
     - `net9.0-windows10.0.19041.0`
     - `net9.0-android`
   - `.github/workflows/dotnet.yml:24-29` still only builds Core/Web and runs Core tests.
   Risk:
   - The MAUI app is much further along, but its build status depends on which target is built and the host environment.
   - CI still gives no signal on MAUI regressions.
   Suggested fix:
   - Add at least a Windows-targeted MAUI build job to CI and document whether full multi-target builds are expected on Windows+OneDrive hosts.

### Low

4. The docs and workflow layer improved materially and are now more explicit than the automation currently enforces.
   Evidence:
   - `docs/ai-workflow.md:41-45` now requires dated maintenance reports.
   - `docs/maintenance-cadence.md:5-19` and `:23-35` now explicitly require official-source checks, external API drift review, and written reports.
   - `docs/maintenance-reviews/README.md:1-31` defines report naming and minimum contents.
   - `.github/prompts/maintenance-review.prompt.md:6-34` now mirrors that stronger maintenance workflow.
   Risk:
   - The process definition is ahead of the automation. This is not a correctness bug, but the repo now depends more heavily on disciplined human review until more checks are automated.
   Suggested fix:
   - Keep the stronger docs, and gradually automate the highest-value parts such as contract drift validation and surface-level smoke checks.

## Requirement -> Code/Test Alignment Summary

- One contract-first model via `openapi.yaml`: `partially_implemented`
  - Evidence: `openapi.yaml` remains the contract source and the code now implements health, non-stream LLM, and streaming LLM flows.
  - Gap: Whisper, usage/admin, and stronger compatibility automation are still absent.

- Shared `.NET` transport/domain logic in `dotnet/src/Core`: `implemented_with_evidence`
  - Evidence: `GatewayClient` now supports health, JSON LLM, and streaming LLM requests.
  - Test evidence: `GatewayClientTests` now pass 5 tests, including streaming cases.

- Parity across web, `.NET Web`, and MAUI clients: `partially_implemented`
  - Evidence: all three surfaces now expose health, non-stream LLM, and streaming LLM flows.
  - Gap: verification parity is not there yet, especially for streaming and MAUI.

- Docs, prompts, and maintenance workflow alignment: `implemented_with_evidence`
  - Evidence: README, `docs/ai-workflow.md`, `docs/maintenance-cadence.md`, `docs/maintenance-reviews/README.md`, and prompt files now describe a coherent operating model.
  - Gap: automation still lags behind those stronger process expectations.

- Tests staying in sync with behavior changes: `partially_implemented`
  - Evidence: Core tests were updated for streaming.
  - Gap: web, `.NET Web`, and MAUI still lack equivalent test coverage.

## Code/Test -> Requirement Coverage Summary

- `dotnet/src/Core`
  - Strongest implementation area.
  - Covers shared health, JSON LLM, and streaming LLM transport logic.

- `dotnet/tests/Core.Tests`
  - Strongest verification area.
  - Covers health parsing, auth header handling, JSON error handling, and streaming request/error behavior.

- `web/`
  - Now supports health, non-stream LLM, and streaming LLM in the UI and client helper layer.
  - Still has no automated tests.

- `dotnet/src/Web`
  - Thin adapter over Core with health, JSON LLM, and stream proxy routes plus a static UI.
  - No direct automated tests.

- `dotnet/src/Maui`
  - UI now supports health, JSON LLM, and streaming LLM.
  - Targeted Windows/Android builds pass; full multi-target build still depends on host/environment behavior.

- Docs/prompts/workflows
  - Healthy and increasingly explicit.
  - Process quality is ahead of enforcement quality.

## Unimplemented or Partial Status List

- Web automated tests: `not_started`
- `.NET Web` route-level tests: `not_started`
- MAUI automated tests: `not_started`
- Whisper flows across clients: `not_started`
- Usage/admin flows across clients: `not_started`
- Contract drift / generated-client automation: `partially_implemented`
- MAUI CI coverage: `not_started`

## Verification Performed

- `npm --prefix web run check`: passed
- `npm --prefix web run build`: passed
- `npm --prefix web test`: failed (`No test files found`)
- `dotnet restore OpenAiServiceClients.slnx`: passed
- `dotnet build dotnet/src/Core/OpenAiServiceClients.Core.csproj --no-restore`: passed
- `dotnet build dotnet/src/Web/OpenAiServiceClients.Web.csproj --no-restore`: passed
- `dotnet test dotnet/tests/Core.Tests/OpenAiServiceClients.Core.Tests.csproj --no-restore`: passed (5 tests)
- `dotnet build dotnet/src/Maui/OpenAiServiceClients.Maui.csproj --no-restore`: failed on iOS/MacCatalyst cleanup (`MSB3231`, access denied in `obj`)
- `dotnet build dotnet/src/Maui/OpenAiServiceClients.Maui.csproj -f net9.0-windows10.0.19041.0 --no-restore`: passed
- `dotnet build dotnet/src/Maui/OpenAiServiceClients.Maui.csproj -f net9.0-android --no-restore`: passed

## Risks, Gaps, and Next Steps

1. Fix the web validation baseline.
   - Web streaming was added without any web tests, so CI is still red for the whole web surface.

2. Add surface-level verification for streaming.
   - Core coverage is good; route/UI coverage is the missing layer now.

3. Add MAUI CI signal.
   - The repo now has meaningful MAUI functionality, so the complete lack of MAUI CI validation is the main blind spot on the `.NET` side.

4. Keep the stronger maintenance workflow.
   - The maintenance docs and prompts are now in good shape; the next step is turning the highest-value pieces into automation where practical.
