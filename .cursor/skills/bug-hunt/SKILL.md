---
name: bug-hunt
description: Reproduces and fixes Taksitle defects with minimal diffs and regression tests. Use for wrong behavior, failing tests, or known HTML-port bugs.
disable-model-invocation: true
---

# Bug Hunt

## When to use
Broken behavior, flaky tests, or porting defects from `fuzul-taksit-takip.html`.

## Instructions
1. Load `.cursor/agents/bug-hunter.md` role and follow its workflow.
2. Reproduce with the smallest fixture (prefer domain unit test).
3. Root-cause in the correct layer (Domain for formulas).
4. Apply a **minimal** fix — no unrelated refactors.
5. Add/adjust a regression test.
6. Re-run the failing test and a nearby golden case (shares/settlement).

## Known HTML bugs (priority when migrating)
- Missing bulk-increase value field
- Orphan payment keys after partner delete
- Pairwise-only settlement for 3+ partners
- Hardcoded print signatures
- XSS via `innerHTML`

## Output
- Root cause
- Fix + files touched
- Regression test
- Verify status
