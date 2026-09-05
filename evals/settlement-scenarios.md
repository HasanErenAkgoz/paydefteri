# Settlement & share-calculation scenarios

Mirrors the edge cases already covered by `tests/PayDefteri.Domain.Tests/DomainAbidikGubidikTests.cs` — restated as scenarios so a reviewer (human or agent) can sanity-check behavior after a change without reading test code.

## 1. Equal split with a repeating decimal

3 partners, `ShareType = Equal`, `TotalAmount = 100`.
- **Expect:** each partner's share is exactly `100/3` (not rounded to 2 decimals mid-calculation), and the three shares still sum to `100`.

## 2. Custom split with a missing partner

`ShareType = Custom`, only partner A has a `CustomShares` entry (`100`); partner B has none.
- **Expect:** partner B's share is `0`, not an exception and not `null`.

## 3. Boundary rounding around the total

`ShareType = Custom` shares summing to `99.99`, `100.01`, `99.98`, `100.02` against a `TotalAmount = 100`.
- **Expect:** `99.99` and `100.01` are accepted as within rounding tolerance; `99.98` and `100.02` are rejected as not summing to the total.

## 4. Settlement when the payer isn't the plan owner

An installment is `paidBy` a partner who is not the plan owner.
- **Expect:** the net-balance calculation treats this as an N-party settlement (the payer is owed by every other partner their share, not just reimbursed by the owner).

## 5. Expense plan vs. installment plan share reuse

An Expense plan (`PlanType.Expense`) and an Installment plan both use the same `ShareType` (default/equal/custom) and settlement math.
- **Expect:** switching a plan's type does not change how an existing installment/expense's share was calculated — the calculators are shared, only the surrounding aggregate behavior differs (see `docs/ADR-001-expense-plans.md`).
