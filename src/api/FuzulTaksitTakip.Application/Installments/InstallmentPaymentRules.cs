namespace FuzulTaksitTakip.Application.Installments;

internal static class InstallmentPaymentRules
{
    /// <summary>
    /// True when the installment's due month is after the current calendar month (UTC).
    /// </summary>
    public static bool IsFutureDueMonth(DateOnly dueDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return dueDate.Year > today.Year
               || (dueDate.Year == today.Year && dueDate.Month > today.Month);
    }
}
