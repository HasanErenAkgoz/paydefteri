using PayDefteri.Domain.Entities;

namespace PayDefteri.Domain.Services;

public static class SettlementCalculator
{
    /// <summary>
    /// N-party netting: if PaidByPartnerId != PartnerId on a paid share,
    /// credit the payer and debit the owner by that share amount.
    /// </summary>
    public static IReadOnlyDictionary<Guid, decimal> ComputeBalances(
        IEnumerable<Installment> installments,
        IReadOnlyList<Partner> partners)
    {
        var balances = partners.ToDictionary(p => p.Id, _ => 0m);

        foreach (var installment in installments)
        {
            foreach (var payment in installment.Payments.Where(p => p.IsPaid))
            {
                if (payment.PaidByPartnerId is null || payment.PaidByPartnerId == payment.PartnerId)
                {
                    continue;
                }

                var owner = partners.FirstOrDefault(p => p.Id == payment.PartnerId);
                if (owner is null)
                {
                    continue;
                }

                var share = ShareCalculator.GetPartnerShare(installment, owner, partners);
                var payerId = payment.PaidByPartnerId.Value;

                if (!balances.ContainsKey(payerId))
                {
                    balances[payerId] = 0m;
                }

                balances[payerId] += share;
                balances[payment.PartnerId] -= share;
            }
        }

        return balances;
    }
}
