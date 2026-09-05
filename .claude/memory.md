# Project Memory

Durable, dated notes on state and decisions — not stable reference (that's [`CLAUDE.md`](../CLAUDE.md) and [`AGENTS.md`](../AGENTS.md)). Update this file when a decision or status changes; keep entries dated so staleness is visible.

## 2026-09-05 — ECC surface curated (agent-sort)

Ran `/ecc:agent-sort`. Full DAILY/LIBRARY classification of the ~450-component ECC catalog for this repo is in [`ecc-agent-sort.md`](./ecc-agent-sort.md); a short pointer version is the `skill-library` project skill (`.claude/skills/skill-library/SKILL.md`). Open questions from that pass: the `orch-*` vs `prp-*` command families overlap and aren't deduplicated; no ECC hook scripts have been wired into `.claude/settings.json` yet (7 candidates identified, none applied without explicit opt-in).

## 2026-09-05 — Harness scaffolding added

Added `.claude/settings.json` (PreToolUse guard, minimal/fail-open), CI (`.github/workflows/ci.yml`), `SECURITY.md`, `.github/dependabot.yml`, PR/issue templates, `CODEOWNERS` (`@HasanErenAkgoz`), and `evals/` with domain acceptance scenarios. Done to close out an ECC harness audit (score went 13/39 → target 39/39 across the 7 applicable categories). None of this changes runtime behavior of the app itself.

## 2026-09-02 — Production readiness: DO NOT SHIP

Full findings in `docs/PRODUCTION-READINESS-AUDIT-2026-09-02.md` (uncommitted as of this writing). Summary:
- **P0 (closed 2026-09-05, `68bdbc2`):** `appsettings.json` had a git-tracked SuperAdmin master password that `IdentityService` accepted as any account's password. The password was rotated, the bypass removed, and the seed admin now comes from an env var / user-secret only.
- **P1s (open):** nginx (10 MB) vs frontend/API (15-16 MB) upload-limit mismatch; Expense Plan import accepts images while Spending Analysis does not (inconsistent UX); no E2E/visual-regression suite; modals lack focus-trap/Escape/focus-restore.
- Recommended fix order is in that doc's "Önerilen Sıra" section — security first, then upload-limit unification, then format parity, then E2E baseline, then a11y, then touch targets, then build-warning cleanup.

## 2026-08-13 — Mobile app: Capacitor, not a separate codebase

Decision recorded in `docs/ADR-003-capacitor-mobile.md`: one Angular codebase wrapped by Capacitor, `appId com.paydefteri.app`, iOS 16+ / Android API 29+. Phased plan (Faz 0-5) in `docs/mobile/IMPLEMENTATION-PLAN.md`. As of this note, `capacitor.config.ts` still allows cleartext traffic and a wildcard `allowNavigation` — acceptable for the current dev phase, must be tightened before the Faz 5 store-submission gate.
