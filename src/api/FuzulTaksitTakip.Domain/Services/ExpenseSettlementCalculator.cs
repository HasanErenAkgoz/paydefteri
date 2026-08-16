using FuzulTaksitTakip.Domain.Entities;
using FuzulTaksitTakip.Domain.Enums;

namespace FuzulTaksitTakip.Domain.Services;

public static class ExpenseSettlementCalculator
{
    /// <summary>
    /// Positive balance = others owe this partner (creditor).
    /// Paid expense: each partner balance += paidAmount - shareAmount.
    /// Transfer From→To: From pays To → credit From, debit To.
    /// </summary>
    public static IReadOnlyDictionary<Guid, decimal> ComputeBalances(
        IEnumerable<Expense> expenses,
        IEnumerable<SettlementTransfer> transfers,
        IReadOnlyList<Partner> partners)
    {
        var balances = partners.ToDictionary(p => p.Id, _ => 0m);

        foreach (var expense in expenses.Where(e => e.Status == ExpenseStatus.Paid))
        {
            var paidByPartner = ResolvePaidAmounts(expense);
            if (paidByPartner.Count == 0)
            {
                continue;
            }

            foreach (var partner in partners)
            {
                var share = ExpenseShareCalculator.GetPartnerShare(expense, partner, partners);
                var paid = paidByPartner.GetValueOrDefault(partner.Id);
                var delta = paid - share;
                if (delta == 0m)
                {
                    continue;
                }

                balances[partner.Id] += delta;
            }
        }

        foreach (var transfer in transfers)
        {
            if (!balances.ContainsKey(transfer.FromPartnerId) || !balances.ContainsKey(transfer.ToPartnerId))
            {
                continue;
            }

            balances[transfer.FromPartnerId] += transfer.Amount;
            balances[transfer.ToPartnerId] -= transfer.Amount;
        }

        return balances;
    }

    /// <summary>
    /// Payments collection wins; otherwise legacy PaidByPartnerId = full TotalAmount.
    /// Zero-amount payment rows are ignored.
    /// </summary>
    public static IReadOnlyDictionary<Guid, decimal> ResolvePaidAmounts(Expense expense)
    {
        var fromPayments = expense.Payments
            .Where(p => p.Amount > 0m)
            .GroupBy(p => p.PartnerId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        if (fromPayments.Count > 0)
        {
            return fromPayments;
        }

        if (expense.PaidByPartnerId is Guid payerId)
        {
            return new Dictionary<Guid, decimal> { [payerId] = expense.TotalAmount };
        }

        return new Dictionary<Guid, decimal>();
    }
}
