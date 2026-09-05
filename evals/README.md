# Evals

Human/agent-checkable acceptance scenarios for behavior that's easy to get subtly wrong — a complement to the automated unit tests in `tests/`, not a replacement. Use these when reviewing a change to settlement math, share allocation, or document import: read the scenario, run the described flow (via the API/UI or a focused test), and confirm the expected outcome still holds.

Add a new scenario file here whenever a bug fix or audit finding reveals an edge case that isn't obvious from the code alone.

## Files

- [`settlement-scenarios.md`](./settlement-scenarios.md) — share-calculation and net-balance edge cases.
- [`import-format-scenarios.md`](./import-format-scenarios.md) — receipt/statement parsing format coverage, including the known Expense-Plan-vs-Spending-Analysis mismatch tracked in `docs/PRODUCTION-READINESS-AUDIT-2026-09-02.md`.
