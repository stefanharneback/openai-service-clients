---
description: Run monthly or quarterly maintenance review for dependency, contract, API, AI/agent, CI, and workflow drift.
agent: reviewer
---

Run a maintenance review using `AGENTS.md`, `README.md`, `openapi.yaml`, `docs/ai-workflow.md`, and `docs/maintenance-cadence.md`.

Cadence: ${input:cadence:Choose monthly or quarterly}

Research requirement:

- For anything that depends on current external state, use internet/web access when available.
- Prefer official documentation, API references, release notes, changelogs, pricing pages, and primary vendor sources.
- If the tool cannot browse the web, state that explicitly in the report and mark the review as partial rather than guessing.

Review:

1. Dependency, SDK, and external API drift in `web` and `dotnet` stacks.
2. OpenAPI contract drift with the gateway repo and any downstream client impact.
3. AI application drift:
   - OpenAI API, model, pricing, and capability changes relevant to this repo
   - other API/platform changes used by these clients
   - whether current patterns for prompting, streaming, retries, structured outputs, auth, and agent/tool usage still fit
4. AI workflow drift:
   - prompts, custom agents, instructions, MCP/editor settings, and evaluation habits
5. CI, docs, and maintenance workflow relevance.

Return:

- create or update a dated report in `docs/maintenance-reviews/` named `YYYY-MM-DD-${input:cadence}.md`
- include review date, cadence, scope, sources checked, commands run, findings, required actions, follow-ups, and limitations
- required maintenance actions now
- quarterly exploration or architecture follow-ups
- risks, assumptions, and commands checked or still to run
