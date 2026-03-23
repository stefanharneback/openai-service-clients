# AI Workflow

This repository keeps AI guidance in layered files so the core workflow stays editor-neutral and portable across VS Code, Copilot, Gemini, Codex, and Antigravity.

## Source of truth

- `AGENTS.md`: primary repository contract
- `README.md`: overview and setup
- `openapi.yaml`: contract source for generated clients and compatibility checks

Editor-specific files refine this guidance and must not replace it.

## Workspace AI files

- `.github/copilot-instructions.md`: repository-wide Copilot instructions
- `.github/instructions/*.instructions.md`: file-pattern guidance
- `.github/prompts/*.prompt.md`: reusable workflows
- `.github/agents/*.agent.md`: custom subagents
- `.vscode/settings.json`: Copilot and chat defaults
- `.vscode/tasks.json`: standard tasks
- `.aiexclude`: local context exclusions

## Recommended loop

1. Plan
- Use the `planner` agent for scoped implementation plans.
- Identify code, tests, docs, and contract impacts.

2. Implement
- Use the `implementer` agent.
- Keep behavior and docs aligned in the same change.

3. Verify
- Run project-specific checks and tests.
- Verify contract compatibility from `openapi.yaml`.

4. Review
- Use the `reviewer` agent with findings-first output.
- Prioritize correctness, regression risk, and missing tests.

## Antigravity note

Treat this file and `AGENTS.md` as canonical workflow references. Tool-specific files are adapters.
