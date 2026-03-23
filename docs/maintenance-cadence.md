# Maintenance Cadence

## Monthly

- Check dependency updates for `web` and `dotnet` projects.
- Verify OpenAPI drift against the gateway repo.
- Review current OpenAI API, model, pricing, and SDK changes that could affect client behavior, docs, or generated types.
- Review other external APIs, SDKs, and platform integrations used by the clients for version, auth, or behavior drift.
- Verify current external changes against official online sources when the reviewing tool supports web access.
- Review AI and agent development guidance for practical drift:
  - prompts, custom agents, and instruction files still match current workflows
  - current approaches for prompting, streaming, retries, structured outputs, and tool/agent usage still fit the repo
- Re-run basic end-to-end scenarios:
  - health
  - llm (json + stream)
  - whisper (upload/url)
  - usage and admin usage
- Capture any changes needed in code, generated clients, docs, or workflow files.
- Write a dated report in `docs/maintenance-reviews/` so the review is visible, reviewable, and timestamped.

## Quarterly

- Reassess architecture boundaries (shared core vs app-specific code).
- Reassess AI application patterns, not just package versions:
  - direct API calls vs agent-style orchestration
  - prompt-only flows vs tool-assisted flows
  - structured outputs, streaming, retry/fallback, and evaluation coverage
  - cost, latency, safety, and observability trade-offs
- Review whether OpenAI API usage and any other external API usage still reflect the best current approach for apps in this repo.
- Review deployment setup for Vercel and .NET hosting targets.
- Review CI path filters and build duration.
- Review model/API roadmap impact and required contract changes.
- Clean up stale docs, scripts, and generated artifacts policies.
- Capture follow-up work as issues or a maintenance summary so design questions do not disappear between reviews.
- Write a dated report in `docs/maintenance-reviews/` with sources, findings, actions, and open questions.
