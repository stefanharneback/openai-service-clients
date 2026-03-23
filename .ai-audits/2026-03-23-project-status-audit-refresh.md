# Project Status Audit Refresh

Date: 2026-03-23
Repository: `openai-service-clients`

## Findings

### High

1. Web CI is still red for any `web/**` change because the configured test command has no matching test files.
   Evidence:
   - `web/package.json:11` defines the web test command as `vitest run`.
   - `.github/workflows/web.yml:24-31` always runs install, type-check, build, and test for web changes.
   - `rg --files web | rg "\.(test|spec)\.(ts|tsx|js|jsx)$"` returned no matches.
   - `npm --prefix web test` currently fails with `No test files found, exiting with code 1`.
   Risk:
   - Any web PR is likely to fail CI even when the code is otherwise valid.
   - The TypeScript client has no regression coverage for the newly added LLM path.
   Suggested fix:
   - Add at least one web smoke test for `/health` and one for `/v1/llm`, or temporarily configure Vitest to tolerate zero tests until real coverage lands.

2. The MAUI client is now wired to the shared Core client, but it is not buildable, and the current CI workflow will not catch that.
   Evidence:
   - `dotnet/src/Maui/MainPage.xaml.cs:13-87` now calls `GatewayClient` for `/health` and `/v1/llm`.
   - `dotnet build dotnet/src/Maui/OpenAiServiceClients.Maui.csproj --no-restore` fails with MAUI-specific compile/configuration errors:
     - missing implicit MAUI package references (`MA002`)
     - missing Windows packaging configuration (`no AppxManifest is specified`)
     - `MauiApplication`/`Run`/`AddDebug` related compile failures from `Program.cs` and `MauiProgram.cs`
   - `.github/workflows/dotnet.yml:24-29` builds only Core and Web and runs Core tests; it never builds the MAUI project.
   Risk:
   - One of the repo’s three client surfaces is currently broken, but the default CI path can still go green.
   Suggested fix:
   - Fix the MAUI project configuration first, then add a dedicated MAUI build job or include MAUI validation in the existing `.NET` workflow.

### Medium

3. Contract coverage has improved, but the implementation still only covers `/health` and non-stream `/v1/llm`.
   Evidence:
   - `openapi.yaml` defines `/health`, `/v1/llm`, `/v1/whisper`, `/v1/usage`, `/v1/admin/usage`, and `/v1/admin/retention`.
   - `web/src/api/client.ts:14-47` covers only `GET /health` and `POST /v1/llm`.
   - `dotnet/src/Core/GatewayClient.cs:19-65` covers only `GetHealthAsync` and `PostLlmAsync`.
   - `dotnet/src/Web/Program.cs:18-57` exposes only `/api/health` and `/api/llm`.
   - `dotnet/src/Maui/MainPage.xaml.cs:13-87` only drives the same two flows.
   Risk:
   - The repo is progressing, but it is still short of parity with the current contract source and maintenance checklist.
   Suggested fix:
   - Prioritize either Whisper or usage/admin support next, and make a deliberate call on whether streaming belongs in V1 for each client surface.

4. Contract automation remains shallow relative to the repo’s stated workflow.
   Evidence:
   - `scripts/sync-openapi.ps1` and `scripts/sync-openapi.sh` only copy `openapi.yaml`.
   - `.github/workflows/contract-sync.yml:18-21` only checks file presence and prints the version line.
   - `docs/maintenance-cadence.md:5-12` expects OpenAPI drift verification and end-to-end scenario reruns.
   Risk:
   - Contract drift can still slip in without generated-client or compatibility enforcement.
   Suggested fix:
   - Add drift detection or generation validation to CI once the client surface area expands further.

### Low

5. README status text now understates the actual implementation progress.
   Evidence:
   - `README.md:35-39` still describes the repo as starter/scaffold/skeleton state only.
   - The current code includes a real shared `.NET` gateway client, `.NET Web` backend routes, and MAUI UI event handlers that call the shared client.
   Risk:
   - New contributors will get an outdated picture of what is implemented.
   Suggested fix:
   - Refresh the README status section after the next feature slice so docs match the code.

## Requirement -> Code/Test Alignment Summary

- One contract-first model via `openapi.yaml`: `partially_implemented`
  - Evidence: `openapi.yaml` is still the central contract source, and client code now implements a subset of it.
  - Gap: no generated clients or CI-based compatibility checks exist yet.

- Shared `.NET` core client for common transport logic: `implemented_with_evidence`
  - Evidence: `GatewayClient`, `GatewayApiException`, `HealthResponse`, and `LlmRequest` are now present in `dotnet/src/Core`.
  - Test evidence: `GatewayClientTests` now validates health parsing, auth header behavior, and error propagation.

- Parity across web, `.NET Web`, and MAUI clients: `partially_implemented`
  - Evidence: all three surfaces now expose health and non-stream LLM flows.
  - Gap: MAUI is not buildable, and no surface covers Whisper, usage, admin, or streaming yet.

- Thin presentation layers over shared logic: `partially_implemented`
  - Evidence: `.NET Web` and MAUI now call into the shared Core client rather than duplicating HTTP logic.
  - Gap: the web TypeScript client is still manually implemented and not contract-generated.

- Tests and docs staying in sync with behavior: `partially_implemented`
  - Evidence: Core tests now exist and pass.
  - Gap: web tests are still missing, MAUI has no automated validation, and README status text lags the code.

## Code/Test -> Requirement Coverage Summary

- `dotnet/src/Core`
  - Strongest current implementation area.
  - Provides reusable health and LLM request logic plus structured error handling.

- `dotnet/tests/Core.Tests`
  - Meaningful improvement over the prior placeholder.
  - Validates the shared Core client without external dependencies.

- `dotnet/src/Web`
  - Now acts as a thin presentation/API adapter over the shared Core client.
  - Covers health and non-stream LLM only.

- `dotnet/src/Maui`
  - Now has real UX/event wiring for health and non-stream LLM.
  - Not currently in a buildable state.

- `web/`
  - Functional manual client for health and non-stream LLM.
  - Still missing tests and broader contract coverage.

- Workflows/docs
  - Good repo structure and separation of concerns.
  - Validation depth still lags the intended operating model.

## Unimplemented or Partial Status List

- Whisper client flows across web / `.NET Web` / MAUI: `not_started`
- Usage and admin usage flows across clients: `not_started`
- Retention/admin maintenance flows in clients: `not_started`
- Streaming response handling in clients: `not_started`
- Web automated tests: `not_started`
- MAUI automated validation/build health in CI: `partially_implemented`
- Contract drift / generated-client automation: `partially_implemented`
- README status refresh: `partially_implemented`

## Verification Performed

- `npm --prefix web run check`: passed
- `npm --prefix web run build`: passed
- `npm --prefix web test`: failed (`No test files found`)
- `dotnet restore OpenAiServiceClients.slnx`: passed with MAUI warnings
- `dotnet build dotnet/src/Core/OpenAiServiceClients.Core.csproj --no-restore`: passed
- `dotnet build dotnet/src/Web/OpenAiServiceClients.Web.csproj --no-restore`: passed
- `dotnet test dotnet/tests/Core.Tests/OpenAiServiceClients.Core.Tests.csproj --no-restore`: passed (3 tests)
- `dotnet build dotnet/src/Maui/OpenAiServiceClients.Maui.csproj --no-restore`: failed (MAUI configuration/compile issues)

## Risks, Gaps, and Next Steps

1. Fix the broken validation baseline.
   - Add web tests or relax the temporary zero-test condition intentionally.
   - Make the MAUI project build cleanly and add CI coverage for it.

2. Expand contract coverage deliberately.
   - The repo now has a usable shared Core layer. The next slice should build on that rather than adding more one-off client code.

3. Tighten docs and automation.
   - Refresh README status text.
   - Strengthen contract-sync from file existence checks to real drift/compatibility enforcement.
