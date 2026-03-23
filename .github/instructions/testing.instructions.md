---
description: Use these guidelines when generating or updating tests.
applyTo: web/**/*.{test,spec}.{ts,tsx,js,jsx},dotnet/tests/**/*.{cs,csproj}
---

## Testing guidelines

- Prefer route/service-level tests for API behavior.
- Cover success and failure paths for auth, validation, streaming, and usage/cost parsing.
- Keep tests deterministic and avoid live external dependencies in unit tests.
- For contract changes, add tests that prove backward/forward compatibility expectations.
- Add at least one end-to-end smoke scenario per client surface.
