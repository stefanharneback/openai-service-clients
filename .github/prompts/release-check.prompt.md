---
description: Run a release readiness review across code, tests, docs, and contract sync.
agent: reviewer
---

Perform a release-readiness check:

1. Verify contract/docs/code consistency.
2. Verify relevant test coverage exists for changed behavior.
3. Verify no secrets or local artifacts are included.
4. Verify build/test commands for changed surfaces are passing.
5. Return a go/no-go verdict with findings.
