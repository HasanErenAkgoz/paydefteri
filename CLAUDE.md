# CLAUDE.md

Project-specific context for Claude Code. See also [`AGENTS.md`](AGENTS.md) for contribution conventions (commands, style, commit/PR guidance) — this file complements it rather than repeating it.

## What this is

PayDefteri: multi-partner debt/installment and shared-expense tracker (Turkish market). Partners split installment plans or day-to-day expense plans, track who paid, and settle net balances. Product name is **PayDefteri**; legacy code namespaces and some file names still say `PayDefteri`/`taksitle` — that's expected, not a bug.

**Stack:** Angular 21 (root `package.json` — README/AGENTS.md still say 19, that's stale) · ASP.NET Core 8 · EF Core · PostgreSQL · JWT · Capacitor (iOS/Android).

## Commands

Standard `docker compose`/`dotnet build`/`dotnet test` and the `npm` scripts in `src/web/package.json` (`start`, `build`, `build:mobile`, `mobile:sync`, `mobile:android`, `mobile:ios`, `test`) all work as-is — check `package.json` for the current script list rather than trusting a copy here. Two things aren't guessable from those sources:

| Action | Command |
|---|---|
| Run the API | `dotnet run --project src/api/PayDefteri.Api` (`http://localhost:5096`, Swagger at `/swagger`) — the only runnable project in the solution |
| New EF migration | `dotnet ef migrations add Name -p src/api/PayDefteri.Infrastructure -s src/api/PayDefteri.Api -o Persistence/Migrations` — the `-p/-s/-o` flags are required for this multi-project layout, `dotnet ef migrations add Name` alone will fail |

## Architecture

Clean Architecture on the API, dependency direction `Domain ← Application ← Infrastructure ← Api`:
- `src/api/PayDefteri.Domain` — entities, enums, calculators (share/settlement math). No framework deps.
- `src/api/PayDefteri.Application` — MediatR commands/queries, one per feature folder (`Plans`, `Expenses`, `Installments`, `Payments`, `Partners`, `Membership`, `Reminders`, `SpendingAnalysis`, `Auth`).
- `src/api/PayDefteri.Infrastructure` — EF Core persistence/migrations, Identity, Documents (receipt/statement parsers), Email, Storage, background jobs.
- `src/api/PayDefteri.Api` — controllers delegate to MediatR only; no business logic here.

Angular app (`src/web/src/app`): `features/*` per screen (plans, expenses, dashboard, spending-analysis, setup, profile, etc.), `shared/*` for reusable UI, `core/*` for guards, interceptors, models, services, and `core/platform` — the web/Capacitor adapter seam (`PlatformPort`, `ConnectivityPort`, `CameraPort`, `DeepLinkPort`, `SharePort`, `SessionStore` per `docs/ADR-003-capacitor-mobile.md`).

Mobile is the same Angular bundle wrapped by Capacitor (`appId com.paydefteri.app`), not a separate codebase — native projects live in `src/web/ios` and `src/web/android`.

## Environment & secrets

- Never add real secrets to `appsettings*.json` — use `dotnet user-secrets set "Section:Key" "value" --project src/api/PayDefteri.Api` locally.
- **Seed admin (audit P0, closed in `68bdbc2`):** `Seed:SuperAdmin:Password` in `src/api/PayDefteri.Api/appsettings.json` is intentionally empty and the master-password login path is gone from `Infrastructure/Identity/IdentityService.cs`. Keep it that way — the seed password comes from an env var / user-secret only, never from a committed file.
- Receipt/statement AI parsing: Gemini primary, OpenAI fallback (`Documents`/`SpendingAnalysis`). Keys via user-secrets locally, `GEMINI_API_KEY`/`OPENAI_API_KEY` env vars in prod (`docker-compose.prod.yml`).
- Prod stack config (`docker-compose.prod.yml`) requires `POSTGRES_PASSWORD`, `JWT_KEY`, `PUBLIC_WEB_URL` (no defaults — compose fails fast if unset); most other vars have sane dev defaults.
- File size limits are **not yet consistent** across layers (nginx 10 MB vs frontend/API 15-16 MB in places) — check the audit doc before changing any upload limit so you fix all layers together, not just one.

## Testing conventions

- Backend: xUnit + FluentAssertions. Name tests by behavior (`Positive_member_can_manage_own_expense_but_not_the_owner_expense`), covering both allowed and rejected paths.
- Some suites are named `*AbidikGubidikTests.cs` (e.g. `DomainAbidikGubidikTests.cs`) — this is an intentional grab-bag/edge-case suite name, not a typo; keep using it for miscellaneous edge cases in that domain rather than renaming or splitting it.
- Frontend has comparatively thin coverage (9 spec files / ~18 tests as of the last audit) and no E2E/visual-regression suite yet — don't assume UI regressions would be caught by `npm test` alone.

## Active workstreams (check before touching related code)

- **Production readiness:** `docs/PRODUCTION-READINESS-AUDIT-2026-09-02.md` (uncommitted) lists a decision of **DO NOT SHIP** with a P0 (hardcoded admin password/master-password bypass) and several P1s (file-size limit mismatch, inconsistent statement-import formats between Expense Plans and Spending Analysis, no accessible-modal focus trap, no E2E baseline). Read it before working in auth, uploads, or modals.
- **Mobile (Capacitor):** phased rollout plan in `docs/mobile/IMPLEMENTATION-PLAN.md` (Faz 0 baseline → Faz 5 closed beta), with companion docs in `docs/mobile/` (`API-CONTRACT.md`, `TEST-STRATEGY.md`, `SECURITY-PRIVACY.md`, `RISK-REGISTER.md`). `capacitor.config.ts` currently allows cleartext traffic and a wildcard `allowNavigation` — expected for dev, must be tightened before store submission (see Faz 5 gate).
