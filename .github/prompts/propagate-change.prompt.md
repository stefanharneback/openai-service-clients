---
description: Propagate a change from this clients repo to downstream repos (multi-agent-task-solver).
---

References:

- [AGENTS.md](../../AGENTS.md)
- [.github/instructions/cross-repo.instructions.md](../instructions/cross-repo.instructions.md)
- [openapi.yaml](../../openapi.yaml)

Change description: ${input:change:Describe the change made in this repo}

## Dependency direction

```
openai-api-service  →  openai-service-clients  →  multi-agent-task-solver
(gateway)              (this repo)                 (MAUI task app)
```

## Step 1 — Classify the change

Determine which categories the change falls into:

- **.NET Core client API change**: new/changed methods in `GatewayClient`, new/changed models
- **Web client change**: changes to `web/src/api/client.ts`
- **OpenAPI contract sync**: `openapi.yaml` was updated from the gateway
- **Shared behavior change**: retry logic, auth handling, error types
- **Documentation-only change**: README, examples, or workflow files

## Step 2 — Determine propagation scope

### .NET Core client API changes

If `dotnet/src/Core/GatewayClient.cs`, models in `dotnet/src/Core/Models/`, or `GatewayApiException.cs` changed:

1. In **multi-agent-task-solver**:
   - Update `src/MultiAgentTaskSolver.Infrastructure/` to use the new client API
   - Update any services or view models that depend on the changed types
   - Update tests in `tests/MultiAgentTaskSolver.Infrastructure.Tests/`
   - Run: `dotnet build MultiAgentTaskSolver.sln && dotnet test MultiAgentTaskSolver.sln --no-build`

### Web client changes

Web client changes typically do not propagate downstream, since multi-agent-task-solver uses the .NET Core client. No action needed unless the change reveals a contract issue that also affects .NET.

### OpenAPI contract sync

If `openapi.yaml` was updated from the gateway:

1. Verify both web and .NET clients still match the updated contract
2. If the .NET Core client needed changes, propagate to multi-agent-task-solver as above
3. If only the web client needed changes, no downstream propagation needed

### Shared behavior changes

If retry, auth, error, or transport logic changed:

1. In **multi-agent-task-solver**:
   - Check if Infrastructure configuration or error handling needs adjustment
   - Check if any UI error display logic needs updating
   - Run: `dotnet build MultiAgentTaskSolver.sln && dotnet test MultiAgentTaskSolver.sln --no-build`

## Step 3 — Validate

Before considering propagation complete, run multi-agent-task-solver's validation:

- `dotnet build MultiAgentTaskSolver.sln`
- `dotnet test MultiAgentTaskSolver.sln --no-build`

## Step 4 — Report

Summarize:
- What changed in this repo
- What was propagated to multi-agent-task-solver
- What validation commands passed
- Any follow-ups or manual steps remaining
