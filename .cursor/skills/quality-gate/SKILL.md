---
name: quality-gate
description: Merge gate for Taksitle — architecture, security, tests, and HTML product parity with an explicit GO/NO-GO. Use before merging a feature branch.
disable-model-invocation: true
---

# Quality Gate

## When to use
Before merge to main. Aggregates architecture, security, verification, and product parity.

## Instructions
1. Confirm `verify-phase` was run (or run it now).
2. Run or simulate reviews:
   - **architecture-reviewer** → layer/tenancy verdict
   - **security-reviewer** → AuthZ/XSS/import verdict
   - **code-reviewer** (if not already done) → Critical findings = 0
3. Product parity checklist vs `fuzul-taksit-takip.html` (for the scoped feature):
   - [ ] Plan / partners / installments CRUD behavior
   - [ ] Share types + status pills
   - [ ] Payment modal fields
   - [ ] Settlement / delivery countdown (if in scope)
   - [ ] Export/import (if in scope)
4. Any Critical architecture/security/test failure → **NO-GO**.

## Output
```markdown
# Quality gate

| Gate | Result |
|------|--------|
| Verify | Pass/Fail |
| Architecture | Pass/Fail |
| Security | Pass/Fail |
| Code review Criticals | 0 / N |
| Product parity (scope) | Pass/Fail |

## Verdict: GO | NO-GO

### Blockers
- ...

### Accepted follow-ups
- ...
```
