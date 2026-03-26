---
description: Run monthly or quarterly maintenance review for dependency, contract, API, AI/agent, CI, and workflow drift.
agent: reviewer
---

References:

- [AGENTS.md](../../AGENTS.md)
- [README.md](../../README.md)
- [openapi.yaml](../../openapi.yaml)
- [docs/ai-workflow.md](../../docs/ai-workflow.md)
- [docs/maintenance-cadence.md](../../docs/maintenance-cadence.md)

Cadence: ${input:cadence:Choose monthly or quarterly}

## Phase 1 — Run verification commands

Run each command below in a terminal. Record the outcome (pass/fail, counts, versions) for the report.

### Web client

1. `npm --prefix web install`
2. `npm --prefix web run check`
3. `npm --prefix web run lint`
4. `npm --prefix web test`
5. `npm --prefix web outdated` — record any outdated packages
6. `npm --prefix web audit --omit=dev --json` — record vulnerability count

### .NET clients

7. `dotnet restore OpenAiServiceClients.slnx`
8. `dotnet build dotnet/src/Core/OpenAiServiceClients.Core.csproj`
9. `dotnet build dotnet/src/Web/OpenAiServiceClients.Web.csproj`
10. `dotnet build dotnet/src/Maui/OpenAiServiceClients.Maui.csproj` (may require MAUI workloads)
11. `dotnet test dotnet/tests/Core.Tests/OpenAiServiceClients.Core.Tests.csproj`
12. `dotnet list dotnet/src/Core/OpenAiServiceClients.Core.csproj package --outdated`

## Phase 2 — Gather current state

Read these files and note their current versions or key values:

- `openapi.yaml` — record the version and endpoint paths
- `web/package.json` — record the version and key dependency versions (React, Vite, TypeScript)
- `dotnet/src/Core/OpenAiServiceClients.Core.csproj` — record the TargetFramework and package versions
- `dotnet/src/Core/GatewayClient.cs` — record the endpoint methods and any hardcoded paths
- `scripts/sync-openapi.ps1` — record what it syncs and validates
- `.github/workflows/web.yml` — record the steps and Node version
- `.github/workflows/contract-sync.yml` — record the gateway ref strategy

## Phase 3 — Check for drift

Research requirement: use internet/web access when available. Prefer official docs and release notes. If web access is unavailable, state that explicitly and mark affected checks as partial.

1. **OpenAPI contract drift**: compare `openapi.yaml` against the gateway repo's current `openapi.yaml`. Note any path, schema, or version mismatches.
2. **Dependency drift**: compare `npm outdated` and `dotnet list package --outdated` output against stable releases.
3. **OpenAI API drift**: check current official models, pricing, and API changes that could affect client behavior.
4. **Other external drift**: check React, Vite, .NET SDK, and MAUI workload versions against current stable releases.
5. **AI/agent workflow drift**: check whether `.github/prompts/`, `.github/instructions/`, `.github/agents/`, `.vscode/`, and `AGENTS.md` still match current best practices.
6. **CI drift**: check whether `.github/workflows/` steps, actions versions, and runtime versions are current.
7. **Client parity**: check endpoint coverage across web, .NET Core, .NET Web, and MAUI clients.

If this is a **quarterly** review, also:

8. Reassess architecture boundaries (shared core vs app-specific code).
9. Reassess AI application patterns (direct API vs orchestration, structured outputs, streaming, retries).
10. Review deployment setup for Vercel and .NET hosting targets.

## Phase 4 — Check previous reviews

Read the most recent report in `docs/maintenance-reviews/` and check:

- Were all required actions from the previous review completed?
- Are any follow-up items still open?

## Phase 5 — Write the report

Create a file named `docs/maintenance-reviews/YYYY-MM-DD-${input:cadence}.md` with this structure:

```markdown
# Maintenance Review Report

- Date: YYYY-MM-DD
- Cadence: ${input:cadence}
- Repo: openai-service-clients
- Reviewer/tool: (your identity)
- Overall outcome: green | amber | red

## Scope

(one-paragraph summary of what this review covers)

## Sources checked

- (list URLs and docs consulted)

## Commands run

- (list each command and its outcome: pass/fail, counts)

## Previous review follow-up

- (status of each action/follow-up from the last review)

## Findings

(numbered list, each with severity, file/area, description, and required action)

## Required maintenance actions

(bulleted list of things that must be done now)

## Recommended follow-ups

(bulleted list of exploration items for next review or next quarter)

## Limitations

- (missing access, partial checks, assumptions)
```

## Checklist

Before finishing, verify:

- [ ] All Phase 1 commands were run and results recorded
- [ ] Key file versions were gathered in Phase 2
- [ ] External and contract drift was checked in Phase 3
- [ ] Previous review follow-ups were checked in Phase 4
- [ ] Report file was created with the correct name and all sections filled
