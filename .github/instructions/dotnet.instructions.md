---
description: Use these guidelines when editing .NET web, MAUI, and shared core projects.
applyTo: dotnet/**/*.{cs,csproj,props,targets,json,config}
---

## Dotnet guidelines

- Keep API transport and DTO logic in `dotnet/src/Core`.
- Keep `.NET Web` and `MAUI` as presentation layers over shared services.
- Keep API compatibility aligned with `openapi.yaml` updates.
- Prefer async APIs and cancellation-token aware methods.
- Keep platform-specific dependencies isolated to app projects.
- Add tests in `dotnet/tests` for parsing, retries, streaming, and error mapping.
