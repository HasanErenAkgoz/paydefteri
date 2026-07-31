using FuzulTaksitTakip.Domain.Entities;
using FuzulTaksitTakip.Domain.Enums;

namespace FuzulTaksitTakip.Domain.Services;

public static class InstallmentStatusCalculator
{
    public static InstallmentStatus Calculate(Installment installment, int partnersCount)
    {
        if (partnersCount <= 0)
        {
            return InstallmentStatus.Pending;
        }

        var paidCount = installment.Payments.Count(p => p.IsPaid);

        if (paidCount == 0)
        {
            return InstallmentStatus.Pending;
        }

        if (paidCount < partnersCount)
        {
            return InstallmentStatus.Partial;
        }

        return InstallmentStatus.Full;
    }
}
