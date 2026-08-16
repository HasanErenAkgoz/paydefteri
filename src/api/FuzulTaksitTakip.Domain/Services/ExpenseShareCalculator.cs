using FuzulTaksitTakip.Domain.Entities;
using FuzulTaksitTakip.Domain.Enums;

namespace FuzulTaksitTakip.Domain.Services;

public static class ExpenseShareCalculator
{
    public static decimal GetPartnerShare(
        Expense expense,
        Partner partner,
        IReadOnlyList<Partner> partners)
    {
        var total = expense.TotalAmount;
        var partnersCount = partners.Count == 0 ? 1 : partners.Count;

        if (expense.ShareType == ShareType.Custom)
        {
            var custom = expense.CustomShares.FirstOrDefault(s => s.PartnerId == partner.Id);
            return custom?.Amount ?? 0m;
        }

        if (expense.ShareType == ShareType.Equal)
        {
            return total / partnersCount;
        }

        return total * partner.DefaultPct / 100m;
    }

    public static bool CustomSharesMatchTotal(Expense expense, decimal tolerance = 0.01m)
    {
        if (expense.ShareType != ShareType.Custom)
        {
            return true;
        }

        var sum = expense.CustomShares.Sum(s => s.Amount);
        return Math.Abs(sum - expense.TotalAmount) <= tolerance;
    }
}
