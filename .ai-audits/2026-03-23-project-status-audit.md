# Project Status Audit

Date: 2026-03-23
Repository: `openai-service-clients`

## Findings

### High

1. Web validation and CI are currently red for any `web/**` change.
   Evidence:
   - `web/package.json:11` defines `npm --prefix web test` as `vitest run`.
   - `.github/workflows/web.yml:30-31` always runs that test command for web changes.
   - The command currently exits with `No test files found, exiting with code 1`.
   Risk:
   - Any PR that touches the web client is likely to fail CI even though the implementation is still scaffold-level.
   - The web client has no regression protection for the only implemented behavior.
   Suggested fix:
   - Add at least one Vitest smoke test for the health-check flow, or temporarily configure Vitest to pass with no tests until real coverage is added.

2. The documented `.NET` onboarding and restore path is self-inconsistent for environments without MAUI workloads.
   Evidence:
   - `README.md:29-30` instructs contributors to run `dotnet restore OpenAiServiceClients.slnx` before `dotnet workload restore`.
   - `AGENTS.md:30-35` repeats the same default command set.
   - `.github/workflows/dotnet.yml:22-23` also restores the whole solution.
   - In the current environment, `dotnet restore OpenAiServiceClients.slnx` fails with `NETSDK1147` because `maui-android` is not installed yet.
   Risk:
   - New contributors cannot follow the documented setup sequence successfully.
   - CI is coupled to runner images already having the needed MAUI workloads.
   Suggested fix:
   - Either move MAUI to a separate solution/workflow, or document and enforce `dotnet workload restore` before any whole-solution restore/build step.

### Medium

3. The repo’s contract-first shared-client architecture is mostly documented, but the implementation is still bootstrap-only.
   Evidence:
   - `README.md:13` and `AGENTS.md:7-10` describe one contract-first model and shared logic across all clients.
   - `dotnet/src/Core/Class1.cs:3-6` is still an empty placeholder.
   - `web/src/api/client.ts:8-14` implements only a manual `/health` fetch.
   - `dotnet/src/Web/Program.cs:4` still returns `"Hello World!"`.
   - `dotnet/src/Maui/MainPage.xaml:10` still shows a starter-shell label.
   Risk:
   - There is not yet a shared transport/domain layer to enforce parity across web, ASP.NET, and MAUI surfaces.
   - The repository is best understood as an initial scaffold, not a functional client suite for the gateway.
   Suggested fix:
   - Implement the shared `.NET` client layer first, then wire `.NET Web` and MAUI to it.
   - Add typed client generation or contract-backed models for the web client rather than ad hoc manual shapes.

4. Contract sync and automated verification are only skeletal today.
   Evidence:
   - `scripts/sync-openapi.ps1:12` just copies the gateway `openapi.yaml`.
   - `.github/workflows/contract-sync.yml:18-21` only verifies file presence and prints the `openapi` version line.
   - `docs/maintenance-cadence.md:6-12` expects OpenAPI drift checks and basic end-to-end scenarios.
   - `.github/instructions/testing.instructions.md:12` expects at least one end-to-end smoke scenario per client surface.
   - `dotnet/tests/Core.Tests/UnitTest1.cs:3-9` is still a placeholder test.
   Risk:
   - Contract drift and client regressions are unlikely to be caught automatically.
   - The repo has workflow structure, but not enough behavioral validation to support the documented process.
   Suggested fix:
   - Add real contract validation or generation checks in CI.
   - Replace placeholder tests with coverage for health, auth failures, request/response mapping, and at least one smoke test per client surface.

## Requirement -> Code/Test Alignment Summary

- One contract-first model via `openapi.yaml`: `partially_implemented`
  - Evidence: `openapi.yaml` defines the service contract and sync scripts exist.
  - Gap: no generated clients or compatibility checks beyond file-presence validation.

- Parity across web, `.NET Web`, and MAUI clients: `not_started`
  - Evidence: all three surfaces exist.
  - Gap: only the web app calls the gateway, and even that only calls `/health`.

- Shared transport/domain logic in `dotnet/src/Core`: `not_started`
  - Evidence: the core project exists and is referenced by the app projects.
  - Gap: the project still contains only `Class1`.

- Thin presentation layers over shared services: `documented_only`
  - Evidence: `.github/copilot-instructions.md:7-8`, `.github/instructions/web.instructions.md:9-13`, and `.github/instructions/dotnet.instructions.md:8-13` all describe this target architecture.
  - Gap: the shared services do not exist yet.

- Tests and docs staying in sync with behavior: `partially_implemented`
  - Evidence: README, AGENTS, workflow docs, and CI files exist.
  - Gap: web tests are absent, dotnet tests are placeholder-only, and CI/README do not yet reflect the MAUI restore prerequisite correctly.

## Code/Test -> Requirement Coverage Summary

- `openapi.yaml`
  - Strong evidence of intended contract scope.
  - No local code generation or client binding yet.

- `web/`
  - Functional starter app with one typed `HealthResponse` and one `fetch` call to `/health`.
  - No coverage for `/v1/llm`, `/v1/whisper`, usage endpoints, auth, or streaming.

- `dotnet/src/Core/`
  - Project wiring is present.
  - No transport, DTO, auth, retry, streaming, or error-mapping implementation exists yet.

- `dotnet/src/Web/`
  - Builds successfully and references Core.
  - Still an ASP.NET starter app with no gateway client integration.

- `dotnet/src/Maui/`
  - Project shell exists and references Core.
  - Still a MAUI starter screen; build is blocked by missing workloads in the current environment.

- `dotnet/tests/Core.Tests/`
  - Test project is wired correctly and executes.
  - Current coverage is effectively zero because the only test is empty.

- CI/workflows
  - Web and dotnet workflows exist and are sensibly split by path.
  - Validation depth is shallow, and current web testing/onboarding expectations do not match repo reality.

## Unimplemented or Partial Status List

- Shared `.NET` API client and DTO layer in `dotnet/src/Core`: `not_started`
- Web typed/generated client beyond `/health`: `not_started`
- `.NET Web` gateway integration: `not_started`
- MAUI gateway integration: `not_started`
- Client coverage for `/v1/llm`, `/v1/whisper`, `/v1/usage`, `/v1/admin/usage`, and `/v1/admin/retention`: `not_started`
- Web automated tests: `not_started`
- Meaningful `.NET` automated tests: `not_started`
- OpenAPI drift/generation automation: `partially_implemented`
- Contributor onboarding for MAUI prerequisites: `partially_implemented`

## Verification Performed

- `npm --prefix web run check`: passed
- `npm --prefix web run build`: passed
- `npm --prefix web test`: failed (`No test files found`)
- `dotnet restore OpenAiServiceClients.slnx`: failed (`NETSDK1147`, missing MAUI workload)
- `dotnet restore dotnet/src/Core/OpenAiServiceClients.Core.csproj`: passed
- `dotnet restore dotnet/src/Web/OpenAiServiceClients.Web.csproj`: passed
- `dotnet restore dotnet/tests/Core.Tests/OpenAiServiceClients.Core.Tests.csproj`: passed
- `dotnet build dotnet/src/Core/OpenAiServiceClients.Core.csproj --no-restore`: passed
- `dotnet build dotnet/src/Web/OpenAiServiceClients.Web.csproj --no-restore`: passed
- `dotnet test dotnet/tests/Core.Tests/OpenAiServiceClients.Core.Tests.csproj`: passed
- `dotnet build dotnet/src/Maui/OpenAiServiceClients.Maui.csproj`: failed (`NETSDK1147`, missing android workload)
- `dotnet workload restore`: timed out in the current environment after roughly four minutes

Note:
- One earlier parallel audit run caused a transient file-lock failure on the Core project; that was a tooling artifact from running build and test at the same time, not a repository defect. Sequential reruns passed.

## Risks, Gaps, and Next Steps

1. Fix the validation baseline first.
   - Make the README and CI reflect the MAUI prerequisite correctly.
   - Decide whether web tests should be mandatory now or whether `passWithNoTests` is an acceptable temporary state.

2. Turn the scaffold into a real shared-client repo.
   - Implement transport, DTOs, auth/error handling, and contract-backed models in `dotnet/src/Core`.
   - Consume that shared layer from `.NET Web` and MAUI.

3. Add minimum meaningful test coverage.
   - Web: health smoke test and one failing-request case.
   - Dotnet Core: request/response mapping and error handling.
   - Cross-surface: at least one smoke scenario per client surface, matching the repo’s own testing guidance.

4. Strengthen contract automation.
   - Detect drift against the gateway repo instead of only checking that `openapi.yaml` exists.
   - Add a generation or compatibility validation step once typed clients exist.
