---
name: plan-phase
description: Plans a Taksitle feature or epic before coding — scope, layers, API/UI checklist, risks, and implementation order. Use when starting a new feature, migration from HTML, or any non-trivial change.
disable-model-invocation: true
---

# Plan Phase

## When to use
New epic/feature, HTML→Angular/.NET parity work, or structural change. Do **not** write production code in this phase.

## Instructions
1. Read `.cursor/settings.json` and `.cursor/rules/architecture.mdc` (+ domain rules as needed).
2. Consult `fuzul-taksit-takip.html` for behavior parity requirements.
3. Optionally delegate design depth to the **architect** agent (`.cursor/agents/architect.md`).
4. Produce a plan with the template below.
5. Stop for user approval before `implement-phase`.

## Output template

```markdown
# Plan: <title>

## Goal
<1-2 sentences>

## In scope / Out of scope
- In: ...
- Out: ...

## HTML parity
- Behaviors preserved: ...
- Bugs to fix while porting: ...

## Layers touched
- Domain / Application / Infrastructure / Api / web features

## API & data
- Endpoints / commands:
- Schema / migrations:

## UI
- Features (dashboard | setup | data):
- Modals / flows:

## Risks
- ...

## Implementation order
1. ...
2. ...

## Test focus
- Golden cases / scenarios:
```

## Next
On approval → `.cursor/skills/implement-phase/SKILL.md`
