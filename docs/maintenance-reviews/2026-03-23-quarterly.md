# Maintenance Review Report

- Date: 2026-03-23
- Cadence: quarterly
- Repo: openai-service-clients
- Reviewer/tool: Codex (GPT-5)
- Overall outcome: amber

## Scope

Quarterly review of client architecture, contract workflow, AI/application patterns, deployment/CI shape, and platform direction.

## Sources checked

- https://platform.openai.com/docs/models
- https://platform.openai.com/docs/api-reference/responses
- https://platform.openai.com/docs/api-reference/audio/createTranscription
- https://learn.microsoft.com/en-us/dotnet/core/releases-and-support

## Commands run

- All commands from the monthly review
- `dotnet build dotnet/src/Maui/OpenAiServiceClients.Maui.csproj --no-restore`
- `Get-Content -Raw .github/workflows/contract-sync.yml`
- `Get-Content -Raw scripts/sync-openapi.ps1`
- `Get-Content -Raw scripts/sync-openapi.sh`

## Findings

1. The `contract-sync` workflow does not actually validate sync with the gateway repo.
   - Current behavior: it checks out the repo, verifies `openapi.yaml` exists, and prints the `openapi:` header line.
   - Impact: the workflow name implies contract parity protection, but it does not compare against `..\\openai-api-service\\openapi.yaml` or invoke the sync scripts.
   - Result: contract-first discipline still depends on manual review.

2. Client capability parity is still shallow relative to the repo's intended surfaces.
   - The web app currently exercises `/health` and non-streaming `/v1/llm`.
   - The shared .NET client exposes health and non-streaming LLM only.
   - The maintenance checklist names health, LLM stream/non-stream, whisper upload/URL, usage, and admin usage, but the client implementations and tests do not yet cover that breadth.
   - Impact: the client repo is still more of a starter workspace than a parity-tested multi-client suite.

3. MAUI verification is not stable across all targets.
   - `dotnet build dotnet/src/Maui/OpenAiServiceClients.Maui.csproj --no-restore` produced Android and Windows artifacts, but failed for iOS and Mac Catalyst with access-denied cleanup errors in `obj\\Debug\\...\\actool`.
   - Impact: the MAUI surface is not yet on a reliable quarterly verification path.

4. The repo is on `.NET 9`, which Microsoft classifies as STS through November 2026.
   - That is acceptable for active development.
   - For distributed client applications, Microsoft explicitly notes that LTS may be preferable when long support windows matter more than newest features.
   - Impact: the repo should document whether `.NET 9` is an intentional STS choice or whether an LTS target is expected later.

5. The current "thin clients over one contract" architecture still makes sense.
   - OpenAPI parity is currently clean.
   - Shared transport logic is centralized in the .NET core client.
   - The quarterly risk is not the architecture itself, but the lack of parity coverage and workflow enforcement around it.

## Required maintenance actions

- Replace or extend `.github/workflows/contract-sync.yml` so it performs an actual contract parity check or sync validation.
- Define a minimum parity matrix for `web`, `.NET Web`, and `.NET MAUI` so quarterly review can measure something real.
- Triage the MAUI iOS/Mac Catalyst cleanup failure before treating MAUI as a healthy validated target.

## Recommended follow-ups

- Decide whether `.NET 9` remains the intentional baseline or whether consumer-facing client targets should move toward an LTS runtime.
- Add stream, whisper, and usage/admin usage scenarios to at least one client surface plus one shared verification path.

## Limitations

- No live gateway endpoint was used, so capability breadth was assessed from code, tests, and build results rather than end-to-end runtime probes.
- External research was limited to official sources.
