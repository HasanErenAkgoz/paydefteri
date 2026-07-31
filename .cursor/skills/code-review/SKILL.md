---
name: code-review
description: Reviews Taksitle diffs against harness rules with severity-tagged findings. Use for PRs, post-implement review, or before quality-gate.
disable-model-invocation: true
---

# Code Review

## When to use
PR open, or user asks to review recent changes before merge.

## Instructions
1. Follow `.cursor/agents/code-reviewer.md`.
2. Inspect the full relevant diff (`git diff` / branch vs main).
3. Enforce checklists from:
   - `architecture.mdc`, `backend.mdc`, `frontend.mdc`
   - `security.mdc`, `testing.mdc`
4. Prioritize: **correctness (money/AuthZ) → security → tests → maintainability**.

## Findings format
| Severity | Item | Location | Fix |
|----------|------|----------|-----|
| Critical | ... | `path` | ... |
| Warning | ... | ... | ... |
| Suggestion | ... | ... | ... |

## Verdict
- **Approve** / **Request changes** / **Block**
- List must-fix items before merge
