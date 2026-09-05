# ECC Agent Sort — paydefteri

Evidence-backed DAILY/LIBRARY classification of the installed ECC catalog (agents, skills, commands, rules, hooks, extras) for this specific repo. Produced via `/ecc:agent-sort` on 2026-09-05 using 4 parallel review-pass subagents (Agents, Commands, Skills×2) plus a direct pass over Rules/Hooks/Extras. Re-run `/ecc:agent-sort` if the stack changes materially (new language, new deploy target, mobile going native, etc.) — this snapshot will drift.

**DAILY** = should be reached for by default every session in this repo. **LIBRARY** = kept accessible (searchable by name/keyword), not a default reach.

## Stack snapshot used for classification

- Backend: ASP.NET Core 8 (C#, nullable enabled), Clean Architecture (Domain/Application/Infrastructure/Api), CQRS via MediatR-style commands/queries, EF Core + PostgreSQL, JWT auth, xUnit + FluentAssertions, Swagger.
- Frontend: Angular 21 (TypeScript, standalone components, SCSS), RxJS, Jasmine/Karma. No ESLint/Prettier config present.
- Mobile: Capacitor 8 wrapping the same Angular app (not native) — iOS + Android projects under `src/web/ios`/`src/web/android`.
- Infra: Docker + docker-compose (dev+prod), Nginx, GitHub (`HasanErenAkgoz/paydefteri`) with GitHub Actions CI, Dependabot. No Vercel/Netlify/Cloudflare/Fly/Kubernetes/Terraform/Redis/GraphQL/gRPC.
- AI: Gemini (primary) + OpenAI (fallback) for receipt/statement document parsing.
- Known open issues: `docs/PRODUCTION-READINESS-AUDIT-2026-09-02.md` — P0 hardcoded admin/master-password auth bypass; upload size-limit mismatches; inconsistent import-format acceptance; no E2E/visual-regression suite; modals missing focus-trap/Escape.
- Not present anywhere: Python, Go, Rust, Java/Kotlin/Spring, PHP/Laravel, Ruby/Rails, native Swift/Android, C/C++, Perl, Vue, React/Next.js, Django, F#, ArkTS/HarmonyOS, blockchain/ML-training/healthcare/prediction-market/network-device domains.

---

## AGENTS (19 DAILY / 49 LIBRARY of 68)

### DAILY
| agent | evidence | why |
|---|---|---|
| a11y-architect | modals missing focus-trap/Escape (tracked P1) | direct hit on a known gap |
| architect | Clean Architecture layering in active use | core to this repo's structure |
| build-error-resolver | dotnet + Angular CLI are the only build systems | generic, matches both toolchains |
| code-architect | layered C#/Angular codebase w/ conventions | feature planning within existing structure |
| code-explorer | multi-layer backend + Angular frontend | tracing cross-layer flows is recurring |
| code-reviewer | C# + TypeScript/Angular are the only languages | stack-agnostic, matches languages present |
| csharp-reviewer | ASP.NET Core 8 C#, nullable enabled | direct language match |
| database-reviewer | EF Core + PostgreSQL | direct persistence-layer match |
| docs-lookup | Angular 21 / EF Core / Capacitor APIs move fast | stack-agnostic docs fetch |
| e2e-runner | tracked gap: no E2E/visual-regression suite | directly addresses documented issue |
| performance-optimizer | Angular render perf + EF Core/Postgres query perf | applies to both layers present |
| planner | active feature work, layered architecture | stack-agnostic planning |
| pr-test-analyzer | CI just added (dotnet+Angular test jobs) | supports PR test-coverage review of actual CI |
| refactor-cleaner | no ESLint/Prettier enforced on frontend | dead-code/consolidation value-add |
| security-reviewer | tracked P0 auth bypass; JWT; uploads | matches known critical risk surface |
| silent-failure-hunter | fragile import/parsing flows (format/size bugs) | matches documented fragile areas |
| tdd-guide | xUnit+FluentAssertions, Jasmine/Karma already in use | matches existing test tooling |
| type-design-analyzer | C# nullable types + TS types | matches both type systems in use |
| typescript-reviewer | Angular 21/TypeScript frontend | direct language match |

### LIBRARY (49)
agent-evaluator, chief-of-staff, code-simplifier, comment-analyzer, conversation-analyzer, cpp-build-resolver, cpp-reviewer, dart-build-resolver, django-build-resolver, django-reviewer, doc-updater, fastapi-reviewer, flutter-reviewer, fsharp-reviewer, gan-evaluator, gan-generator, gan-planner, go-build-resolver, go-reviewer, harmonyos-app-resolver, harness-optimizer, healthcare-reviewer, homelab-architect, java-build-resolver, java-reviewer, kotlin-build-resolver, kotlin-reviewer, loop-operator, marketing-agent, mle-reviewer, network-architect, network-config-reviewer, network-troubleshooter, opensource-forker, opensource-packager, opensource-sanitizer, php-reviewer, python-reviewer, pytorch-build-resolver, rag-pipeline-reviewer, react-build-resolver, react-reviewer, rust-build-resolver, rust-reviewer, seo-specialist, spec-miner, swift-build-resolver, swift-reviewer, vue-reviewer

All off-stack (wrong language/framework/domain) or generic-but-unevidenced (agent-evaluator, comment-analyzer, doc-updater, harness-optimizer, loop-operator).

---

## COMMANDS (23 DAILY / 71 LIBRARY of 94)

### DAILY
aside, build-fix, checkpoint, code-review, feature-dev, harness-audit, orch-add-feature, orch-change-feature, orch-fix-defect, orch-refine-code, orch-review, plan, plan-prd, pr, prp-commit, prp-implement, prp-plan, prp-pr, prp-prd, refactor-clean, review-pr, security-scan, test-coverage

**⚠ Open question:** the `orch-*` family (add-feature/change-feature/fix-defect/refine-code/review) and the `prp-*` family (commit/implement/plan/pr/prd) look like two competing "plan → implement → verify → PR" pipelines covering overlapping ground, both rated DAILY here on generic stack-agnostic merit. Worth picking one family (or clarifying when to reach for which) rather than keeping 10 overlapping entry points — see `skill-stocktake` handoff below.

### LIBRARY (71)
auto-update, cost-report, cpp-build, cpp-review, cpp-test, ecc-guide, epic-claim, epic-decompose, epic-publish, epic-review, epic-sync, epic-unblock, epic-validate, evolve, fastapi-review, flutter-build, flutter-review, flutter-test, gan-build, gan-design, go-build, go-review, go-test, gradle-build, hookify-configure, hookify-help, hookify-list, hookify, instinct-export, instinct-import, instinct-status, jira, kotlin-build, kotlin-review, kotlin-test, learn-eval, learn, loop-start, loop-status, marketing-campaign, model-route, multi-backend, multi-execute, multi-frontend, multi-plan, multi-workflow, orch-build-mvp, plan-canvas, pm2, project-init, projects, promote, prune, python-review, quality-gate, react-build, react-review, react-test, resume-session, rust-build, rust-review, rust-test, santa-loop, save-session, sessions, setup-pm, skill-create, skill-health, update-codemaps, update-docs, vue-review

Note: one subagent pass flagged its own output as containing repeated literal references to `.claude/settings.json` (used as evidence text for the hookify-* rows); reviewed manually — plain evidence citations, not embedded instructions.

---

## SKILLS (16 DAILY / 270 LIBRARY of 286)

### DAILY
| skill | evidence |
|---|---|
| accessibility | modals missing focus-trap/Escape (tracked P1) |
| angular-developer | frontend is Angular 21 |
| api-design | ASP.NET Core REST API (CQRS/MediatR) |
| coding-standards | no ESLint/Prettier config; C#/Angular both need a quality floor |
| csharp-testing | xUnit + FluentAssertions |
| database-migrations | EF Core + PostgreSQL, ongoing schema migrations |
| deployment-patterns | Docker+compose (dev/prod), Nginx, GitHub Actions CI |
| docker-patterns | Docker + docker-compose explicitly used |
| documentation-lookup | multi-framework repo (Angular/EF Core/Capacitor) |
| dotnet-patterns | ASP.NET Core 8 C#, nullable, DI, async |
| git-workflow | standard git/PR-based repo |
| github-ops | GitHub Actions CI, Dependabot, PR/issue templates, CODEOWNERS all present |
| postgres-patterns | EF Core + PostgreSQL |
| security-review | JWT auth, uploads, tracked P0 auth bypass |
| tdd-workflow | xUnit+FluentAssertions + Jasmine/Karma |
| verification-loop | CI + known open issues to verify against |

**⚠ Naming collision to know about:** a skill literally named `security-scan` was rated **LIBRARY** ("audits the `.claude/` harness config itself, not application security") — different from the `security-scan` **command** rated DAILY above (app-security scan). Same name, different surface, different verdict; both are correct for what they actually are.

### LIBRARY (270)
Everything else — off-stack languages/frameworks (Python/Django/FastAPI, Go, Rust, Java/Spring/Quarkus, PHP/Laravel, Ruby/Rails, native Swift/Kotlin/Android, Vue/Nuxt, React/Next.js, F#, ArkTS/HarmonyOS, C/C++, Perl), off-domain business/ops skills (healthcare, prediction-markets, blockchain/DeFi, logistics, energy, homelab/networking, marketing/SEO/social, scientific/PubMed/USPTO), and ECC's own meta-tooling (skill-stocktake, skill-scout, skill-comply, agent-sort itself, config-gc, ecc-guide, continuous-learning, instinct-*, etc.). Full per-skill evidence rows are in the two subagent transcripts this file was built from — re-run `/ecc:agent-sort` to regenerate if a fresh full dump is needed.

Adjacent-but-not-promoted, worth knowing about: `e2e-testing` (Playwright — relevant to the tracked E2E gap, but not a default daily load), `error-handling` (covers TS/Python/Go, not C#, so only half-applicable).

---

## RULES (5 dirs DAILY / 17 dirs LIBRARY of 22, ~32/~90 files)

### DAILY
`common` (10 files), `csharp` (5), `typescript` (5), `angular` (5), `web` (7) — path-glob-scoped (e.g. angular rules match `**/*.component.ts`), so they already only fire on matching files; no repo-side action needed beyond confirming these are the right ones.

### LIBRARY
`arkts`, `cpp`, `dart`, `fsharp`, `golang`, `java`, `kotlin`, `nuxt`, `perl`, `php`, `python`, `react`, `react-native`, `ruby`, `rust`, `swift`, `vue` — no matching file globs in this repo.

---

## HOOKS / SCRIPTS (recommended, not yet wired)

`.claude/settings.json` already has one custom hook (`guard-destructive-bash.js`, added this session for the harness-audit pass) — not ECC's own hook system. The following ECC hook scripts (`scripts/hooks/*.js`) are evidenced as good fits **if** the user wants to opt in — **not applied automatically**, since wiring a hook changes real per-tool-call behavior for every future session:

| script | why it'd fit |
|---|---|
| `block-no-verify.js` | reinforces the existing git-safety rule (never skip hooks) at the tool level |
| `pre-bash-commit-quality.js` | generic pre-commit quality check |
| `pre-bash-git-push-reminder.js` | low-risk reminder, no blocking |
| `post-edit-typecheck.js` | runs a TS check after editing `.ts`/`.tsx` — matches the Angular frontend |
| `check-console-log.js` / `post-edit-console-warn.js` | flags leftover `console.log` in JS/TS — useful given no lint is enforced |
| `doc-file-warning.js` | warns before creating ad-hoc doc files — reinforces existing "don't create docs unless asked" default |
| `suggest-compact.js` | generic context-pressure nudge |

Already active **globally** (observed firing repeatedly this session via the user's own `~/.claude/settings.json`, not project-specific): `gateguard-fact-force.js`, `gateguard-heredoc.js` — no repo action needed, they're not part of this project's install.

Everything else in `scripts/hooks/` (~38 remaining scripts) is LIBRARY: Biome-dependent (`post-edit-format.js`, `quality-gate.js`, `config-protection.js` — no Biome/ESLint/Prettier config exists yet), dispatcher plumbing only relevant if adopting ECC's full `hooks.json` (`*-dispatcher.js`), the fuller ECC cross-session memory system (`session-start*.js`, `session-end*.js` — this repo already has its own lightweight `.claude/memory.md`), and assorted ops/telemetry hooks (`cost-tracker.js`, `desktop-notify.js`, `mcp-health-check.js`, `ecc-statusline.js`, etc.) not evidenced as needed.

---

## EXTRAS — all LIBRARY / not applicable

- `contexts/` (dev.md, research.md, review.md) — generic operating-mode docs, no repo-specific tie.
- `examples/` (~47 files) — per-stack example `CLAUDE.md`s (Django, Go, Laravel, Rails, Rust, Next.js, HarmonyOS) — none matches this repo's ASP.NET Core+Angular+Capacitor combination.
- `mcp-configs/mcp-servers.json` — no additional MCP servers needed beyond what's already globally configured.
- `config/project-stack-mappings.json`, `manifests/install-*.json` — ECC's own selective-install tooling; used as a cross-check input for this classification, not project content.
- `workflows/orch-review.workflow.js` — no evidence the Workflow tool is in use on this repo.
- `integrations/aura` — Python threat-model adapter, off-stack and unused.
- `plugins/ecc`, `docs/`, `assets/`, `scaffolds/`, `schemas/`, `research/` — ECC's own internal packaging/repo content, not project material.

---

## Open questions for the user

1. **orch-\* vs prp-\* command families** — both rated DAILY on generic merit but overlap heavily. Pick one, or hand off to `skill-stocktake` for an overlap-cleanup pass.
2. **Hook wiring** — want any of the 7 recommended `scripts/hooks/*.js` actually added to `.claude/settings.json`? Not done automatically since it changes tool-call behavior every session.
3. **`skill-library` router** — a `.claude/skills/skill-library/SKILL.md` was added alongside this file (see below) so a future session can quickly recall the DAILY set and where to find LIBRARY items by keyword, without re-reading this whole file.
