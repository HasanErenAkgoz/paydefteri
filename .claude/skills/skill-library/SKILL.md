---
name: skill-library
description: Curated DAILY vs LIBRARY map of the installed ECC agent/skill/command/rule/hook catalog, scoped to this repo's actual stack (ASP.NET Core/C#, Angular/TypeScript, Capacitor mobile, Docker, PostgreSQL). Use to quickly recall which ECC surfaces are a default reach for this repo, and where to look for something more niche.
metadata:
  type: reference
---

# Skill Library (paydefteri)

Full evidence-backed classification lives in [`../../ecc-agent-sort.md`](../../ecc-agent-sort.md) (produced by `/ecc:agent-sort` on 2026-09-05). This file is the short pointer — read the full file when you need evidence or the complete LIBRARY listing.

## DAILY — reach for these first in this repo

- **Agents:** a11y-architect, architect, build-error-resolver, code-architect, code-explorer, code-reviewer, csharp-reviewer, database-reviewer, docs-lookup, e2e-runner, performance-optimizer, planner, pr-test-analyzer, refactor-cleaner, security-reviewer, silent-failure-hunter, tdd-guide, type-design-analyzer, typescript-reviewer
- **Skills:** accessibility, angular-developer, api-design, coding-standards, csharp-testing, database-migrations, deployment-patterns, docker-patterns, documentation-lookup, dotnet-patterns, git-workflow, github-ops, postgres-patterns, security-review, tdd-workflow, verification-loop
- **Commands:** aside, build-fix, checkpoint, code-review, feature-dev, harness-audit, orch-add-feature, orch-change-feature, orch-fix-defect, orch-refine-code, orch-review, plan, plan-prd, pr, prp-commit, prp-implement, prp-plan, prp-pr, prp-prd, refactor-clean, review-pr, security-scan, test-coverage
- **Rules:** common, csharp, typescript, angular, web (path-glob-scoped, already fire only on matching files)

Two DAILY families overlap and haven't been deduplicated yet: `orch-*` and `prp-*` commands both cover "plan → implement → verify → PR." Pick whichever fits the moment; see the open question in `ecc-agent-sort.md` if you want to prune one.

## LIBRARY — reachable by keyword, not a default load

Everything off-stack for this repo: any Python/Go/Rust/Java/PHP/Ruby/native-Swift/native-Kotlin/Vue/React/Next.js/F#/HarmonyOS/C++/Perl content, and off-domain business/ops skills (healthcare, blockchain, logistics, marketing/SEO, networking/homelab, scientific databases). Also ECC's own meta-tooling (skill-stocktake, skill-scout, continuous-learning, instinct-*, etc.) — useful for maintaining ECC itself, not for building this app.

If you need one of these, just invoke it by name (`ecc:<skill-name>` / `/ecc:<command-name>`) — nothing needs to be "installed" first, the whole catalog is already available via the plugin. This router only exists to save a full re-scan of ~450 components every session.

## When this goes stale

Re-run `/ecc:agent-sort` if: a new language/framework is introduced, the mobile app moves off Capacitor to native, a new deploy target is added (Vercel/Netlify/Cloudflare/Fly/K8s), or linting/formatting tooling (ESLint/Prettier/Biome) is finally added to the frontend — several LIBRARY items (e.g. `config-protection.js`, `quality-gate.js`, `post-edit-format.js` hooks) are only waiting on that last one.
