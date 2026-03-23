# Maintenance Cadence

## Monthly

- Check dependency updates for `web` and `dotnet` projects.
- Verify OpenAPI drift against the gateway repo.
- Re-run basic end-to-end scenarios:
  - health
  - llm (json + stream)
  - whisper (upload/url)
  - usage and admin usage
- Verify prompts, agents, and instruction files still match current practices.

## Quarterly

- Reassess architecture boundaries (shared core vs app-specific code).
- Review deployment setup for Vercel and .NET hosting targets.
- Review CI path filters and build duration.
- Review model/API roadmap impact and required contract changes.
- Clean up stale docs, scripts, and generated artifacts policies.
