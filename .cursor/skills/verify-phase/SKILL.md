---
name: verify-phase
description: Verifies Taksitle changes via builds, automated tests, and Turkish manual scenarios including Fuzul seed totals. Use after implement-phase.
disable-model-invocation: true
---

# Verify Phase

## When to use
Implementation of a slice/feature is complete. Confirm it works before review/merge.

## Instructions
1. Follow `.cursor/rules/testing.mdc`.
2. Delegate test authorship gaps to **test-engineer** if coverage is thin.
3. Run available commands from `.cursor/settings.json` (update if paths differ):
   - `dotnet build` / `dotnet test`
   - `npm test` (web)
   - Playwright smoke if UI flow changed
4. Manual TR scenarios (when UI/API up):

| Scenario | Expect |
|----------|--------|
| Fuzul seed load | Grand total **1.070.000 ₺**, 20 installments |
| 50/50 default share on 25.000 | 12.500 each |
| Mark one partner paid | Status → partial; metrics update |
| Pay-on-behalf | Settlement card shows internal debt |
| Export/import JSON | Round-trip preserves plan |
| Unauthorized planId | 401/403 |

5. Record failures; send math/AuthZ bugs to **bug-hunter**.

## Output
```markdown
# Verify report
- Commands run:
- Automated results:
- Manual results:
- Blockers:
- Ready for quality-gate: yes/no
```
