---
description: Use these guidelines when editing the TypeScript Vercel web client.
applyTo: web/**/*.{ts,tsx,js,jsx,json,yaml,yml}
---

## Web client guidelines

- Keep framework-specific code inside `web/` only.
- Keep API calls behind a dedicated client service layer.
- Prefer typed request/response models generated from `openapi.yaml`.
- Do not duplicate business logic from `.NET` shared core unless platform-specific.
- Keep environment variables documented and validated.
- Add focused tests for request builders, API adapters, and UI state transitions.
