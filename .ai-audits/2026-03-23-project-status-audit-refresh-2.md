# Project Status Audit Refresh 2

Date: 2026-03-23
Repository: `openai-service-clients`

## Findings

### High

1. The web validation path is still red because the configured test step has no matching test files.
   Evidence:
   - `web/package.json:11` defines `vitest run`.
   - `.github/workflows/web.yml:24-31` always runs install, type-check, build, and test for `web/**` changes.
   - `npm --prefix web test` still fails with `No test files found`.
   Risk:
   - Any web PR is likely to fail CI even when the code is otherwise valid.
   - The new `/v1/llm` path in the web client still has zero automated protection.
   Suggested fix:
   - Add at least one web test for `/health` and one for `/v1/llm`, or explicitly configure a temporary zero-test policy until web tests land.

### Medium

2. MAUI validation improved significantly, but full multi-target build health is still incomplete and not covered by CI.
   Evidence:
   - `dotnet/src/Maui/OpenAiServiceClients.Maui.csproj:14` now sets `WindowsPackageType=None`, and `:27-28` adds `Microsoft.Maui.Controls` plus the Core project reference.
   - Platform entrypoints now exist under `dotnet/src/Maui/Platforms/**`.
   - Targeted builds now pass for:
     - `net9.0-windows10.0.19041.0`
     - `net9.0-android`
   - Full `dotnet build dotnet/src/Maui/OpenAiServiceClients.Maui.csproj --no-restore` still fails in the current environment on iOS/MacCatalyst cleanup with `MSB3231` access-denied errors under `obj\Debug\...`.
   - `.github/workflows/dotnet.yml:24-29` still skips MAUI build validation entirely.
   Risk:
   - The MAUI project is much closer to healthy, but cross-target validation remains fragile and CI still won’t catch MAUI regressions.
   Suggested fix:
   - Add at least one targeted MAUI build to CI, preferably `net9.0-windows10.0.19041.0` on Windows runners.
   - Decide whether iOS/MacCatalyst builds are expected in this Windows/OneDrive environment, and document that explicitly.

3. Contract coverage is still only partial relative to `openapi.yaml`.
   Evidence:
   - `openapi.yaml` defines `/health`, `/v1/llm`, `/v1/whisper`, `/v1/usage`, `/v1/admin/usage`, and `/v1/admin/retention`.
   - The web client (`web/src/api/client.ts`) covers only `GET /health` and non-stream `POST /v1/llm`.
   - The shared Core client (`dotnet/src/Core/GatewayClient.cs`) covers only the same two operations.
   - `.NET Web` and MAUI both route through that same subset.
   Risk:
   - The repo is now meaningfully implemented, but it still does not match the full current gateway contract or the maintenance checklist scenarios.
   Suggested fix:
   - Pick the next surface area deliberately: Whisper, usage/admin endpoints, or streaming.

4. Contract automation remains shallow.
   Evidence:
   - `scripts/sync-openapi.ps1` and `scripts/sync-openapi.sh` still only copy `openapi.yaml`.
   - `.github/workflows/contract-sync.yml:18-21` still only checks file presence and prints the version line.
   - `docs/maintenance-cadence.md:5-12` expects drift checks and scenario reruns.
   Risk:
   - Contract drift can still slip through without generation or compatibility enforcement.
   Suggested fix:
   - Add a drift or generation validation step once the next client slice is implemented.

### Low

5. README status text still understates the current implementation.
   Evidence:
   - `README.md:35-39` still describes the repo as starter/scaffold/skeleton state.
   - The current code now includes a reusable shared Core client, `.NET Web` API/UI wiring, MAUI platform entrypoints, and reviewer/maintenance/release prompt files that align with the repository workflow.
   Risk:
   - New contributors get an outdated picture of what is already working.
   Suggested fix:
   - Refresh the README status section after the next feature slice or release-readiness pass.

## Requirement -> Code/Test Alignment Summary

- One contract-first model via `openapi.yaml`: `partially_implemented`
  - Evidence: `openapi.yaml` remains the source contract and the code now implements a meaningful subset.
  - Gap: no generated clients or CI compatibility checks exist yet.

- Shared `.NET` transport/domain logic in `dotnet/src/Core`: `implemented_with_evidence`
  - Evidence: `GatewayClient`, `GatewayApiException`, `HealthResponse`, and `LlmRequest` now exist and are consumed by `.NET Web` and MAUI.
  - Test evidence: `GatewayClientTests` now pass and cover parsing, auth header handling, and error propagation.

- Parity across web, `.NET Web`, and MAUI clients: `partially_implemented`
  - Evidence: all three surfaces now implement health and non-stream LLM flows.
  - Gap: Whisper, usage/admin, and streaming are still absent; MAUI multi-target validation is still incomplete.

- Thin UI layers over shared services: `partially_implemented`
  - Evidence: `.NET Web` and MAUI now delegate to the shared Core client.
  - Gap: the TypeScript web client is still hand-authored rather than contract-generated.

- Docs, prompts, workflows, and tests staying in sync: `partially_implemented`
  - Evidence: prompt files and workflow docs are coherent and aligned with review/maintenance/release practices.
  - Gap: README status text lags the code, web tests are still missing, and contract-sync remains shallow.

## Code/Test -> Requirement Coverage Summary

- `dotnet/src/Core`
  - Best current implementation area.
  - Provides reusable health and non-stream LLM transport logic with structured error handling.

- `dotnet/tests/Core.Tests`
  - Healthy and meaningful for the current Core scope.
  - Still limited to the two implemented Core operations.

- `dotnet/src/Web`
  - Thin adapter over Core plus a simple browser UI.
  - Covers the same two gateway flows only.

- `dotnet/src/Maui`
  - Major progress: shared client wiring, UI events, and platform entrypoints are present.
  - Targeted Windows/Android builds pass; full multi-target build is still environment-fragile.

- `web/`
  - Functional manual client for health and non-stream LLM.
  - Still lacks tests and broader contract coverage.

- `.github/prompts`, `.github/agents`, `.github/instructions`
  - Strong repo hygiene layer.
  - Release, maintenance, and review prompts align with the AGENTS/workflow model.

## Unimplemented or Partial Status List

- Web automated tests: `not_started`
- Whisper flows across clients: `not_started`
- Usage/admin flows across clients: `not_started`
- Streaming client support: `not_started`
- Generated client or compatibility automation from `openapi.yaml`: `partially_implemented`
- MAUI multi-target validation in CI: `partially_implemented`
- README status refresh: `partially_implemented`

## Verification Performed

- `npm --prefix web run check`: passed
- `npm --prefix web run build`: passed
- `npm --prefix web test`: failed (`No test files found`)
- `dotnet restore OpenAiServiceClients.slnx`: passed
- `dotnet build dotnet/src/Core/OpenAiServiceClients.Core.csproj --no-restore`: passed
- `dotnet build dotnet/src/Web/OpenAiServiceClients.Web.csproj --no-restore`: passed
- `dotnet test dotnet/tests/Core.Tests/OpenAiServiceClients.Core.Tests.csproj --no-restore`: passed (3 tests)
- `dotnet build dotnet/src/Maui/OpenAiServiceClients.Maui.csproj --no-restore`: failed on iOS/MacCatalyst cleanup (`MSB3231`, access denied in `obj`)
- `dotnet build dotnet/src/Maui/OpenAiServiceClients.Maui.csproj -f net9.0-windows10.0.19041.0 --no-restore`: passed
- `dotnet build dotnet/src/Maui/OpenAiServiceClients.Maui.csproj -f net9.0-android --no-restore`: passed

## Risks, Gaps, and Next Steps

1. Fix the validation baseline on the web side.
   - Right now web CI still fails by design because tests are missing.

2. Add explicit MAUI CI coverage.
   - The MAUI project is now much healthier, but the default workflow still gives no signal on it.

3. Expand contract coverage intentionally.
   - The repo now has a usable shared Core client. The next feature slice should reuse that foundation instead of widening surface-specific ad hoc code.

4. Refresh docs and strengthen contract automation.
   - The prompt/workflow layer is in good shape; the README status and contract-sync depth are the main lagging areas.
