---
name: implement-phase
description: Implements an approved PayDefteri plan in Backend→DB→API→Frontend order with small diffs and harness rule compliance. Use after plan-phase approval.
disable-model-invocation: true
---

# Implement Phase

## When to use
A plan from `plan-phase` (or equivalent) is approved. Do not expand scope.

## Instructions
1. Re-read the approved plan and relevant rules (`backend`, `frontend`, `database`, `security`).
2. Implement in this order unless the plan says otherwise:
   1. Domain + Application (math, validators)
   2. Infrastructure / EF migrations
   3. Api endpoints + AuthZ
   4. Angular feature UI
   5. Tests alongside behavior (or immediately after each slice)
3. Delegate as needed:
   - **backend-developer** for .NET work
   - **frontend-developer** for Angular work
4. Keep PRs/diffs small and focused.
5. Do not commit unless the user asks.

## Guardrails
- Share/settlement formulas must match `.cursor/rules/backend.mdc`
- No business math only in Angular templates
- No secrets in the tree
- Fix known port bugs if they fall in scope (bulk value, orphan keys, N-party settlement)

## Done when
- Plan checklist items for this slice are implemented
- Build succeeds for touched projects (when solution exists)
- Ready for `verify-phase`
