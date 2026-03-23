# Copilot Repository Instructions

Use `AGENTS.md` as the primary repository contract and `docs/ai-workflow.md` as the workflow reference.

- This is a multi-client monorepo for the OpenAI gateway service.
- Preserve shared contract parity across `web`, `.NET Web`, and `.NET MAUI` clients.
- Keep one source of API truth via `openapi.yaml` and generated clients.
- Prefer thin UI layers and shared transport/domain logic.
- When changing API-facing behavior, update implementation, tests, docs, and prompts/agents together.
- Do not commit secrets, API keys, local overrides, or editor-private state.
- Before finishing code changes, run relevant checks for affected folders.
