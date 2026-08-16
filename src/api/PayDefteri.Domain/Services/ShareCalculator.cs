using PayDefteri.Domain.Entities;
using PayDefteri.Domain.Enums;

namespace PayDefteri.Domain.Services;

public static class ShareCalculator
{
    public static decimal GetPartnerShare(
        Installment installment,
        Partner partner,
        IReadOnlyList<Partner> partners)
    {
        var total = installment.TotalAmount;
        var partnersCount = partners.Count == 0 ? 1 : partners.Count;

        if (installment.ShareType == ShareType.Custom)
        {
            var custom = installment.CustomShares.FirstOrDefault(s => s.PartnerId == partner.Id);
            return custom?.Amount ?? 0m;
        }

        if (installment.ShareType == ShareType.Equal)
        {
            return total / partnersCount;
        }

        return total * partner.DefaultPct / 100m;
    }

    public static bool CustomSharesMatchTotal(Installment installment, decimal tolerance = 0.01m)
    {
        if (installment.ShareType != ShareType.Custom)
        {
            return true;
        }

        var sum = installment.CustomShares.Sum(s => s.Amount);
        return Math.Abs(sum - installment.TotalAmount) <= tolerance;
    }

    public static bool DefaultPercentagesSumTo100(IEnumerable<Partner> partners, decimal tolerance = 0.01m)
    {
        var sum = partners.Sum(p => p.DefaultPct);
        return Math.Abs(sum - 100m) <= tolerance;
    }
}
