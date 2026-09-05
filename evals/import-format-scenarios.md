# Document import format scenarios

Tracks the known inconsistency from `docs/PRODUCTION-READINESS-AUDIT-2026-09-02.md` so it stays visible until fixed, plus baseline coverage for the receipt/statement import paths.

## 1. Expense Plan statement import accepts images

Path: `src/web/.../expenses/statement-import/statement-import-modal.component.html`.
- **Current:** accepts PDF, XLSX, CSV, JPEG, PNG, WebP.
- **Expect once fixed:** format list matches whatever product decision is made in the audit's exit criterion — either Spending Analysis gains image support, or this screen's format list is explicitly pared down and the discrepancy is documented in-product, not just in code.

## 2. Spending Analysis rejects images

Path: `src/web/.../spending-analysis/spending-analysis.component.html` + `.ts` (`:230`, `:236`).
- **Current:** only CSV, XLSX, PDF accepted; images rejected in code even if selected.
- **Expect:** the same file, uploaded to both screens, should get the *same* accept/reject decision once the audit's P1 is resolved.

## 3. Upload size at the layer boundary

- **Current:** nginx (`src/web/deploy/nginx.conf`) caps request bodies at 10 MB; several frontend/API paths allow up to 15-16 MB.
- **Scenario:** upload a 9.9 MB file (should succeed end-to-end), a 10.1 MB file (should fail predictably — not with a raw `413` the app doesn't explain), and a file at whatever the unified limit becomes once the audit's P1 is resolved.

## 4. Receipt analysis provider fallback

Path: `src/api/PayDefteri.Infrastructure/Documents` (Gemini primary, OpenAI fallback per `README.md`).
- **Scenario:** simulate a Gemini technical failure (timeout/5xx) and confirm the request falls back to OpenAI rather than surfacing a hard error to the user.
