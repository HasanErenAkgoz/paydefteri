namespace FuzulTaksitTakip.Application.Common.Interfaces;

public sealed record PartnerPaymentReminderEmail(
    string ToEmail,
    string RecipientName,
    string PlanTitle,
    Guid PlanId,
    string InstallmentName,
    DateOnly DueDate,
    decimal AmountDue,
    bool IsOverdue,
    int OffsetDays);

public sealed record OwnerPaymentReminderEmail(
    string ToEmail,
    string PlanTitle,
    Guid PlanId,
    string InstallmentName,
    DateOnly DueDate,
    bool IsOverdue,
    int OffsetDays,
    IReadOnlyList<OwnerReminderUnpaidRow> UnpaidPartners);

public sealed record OwnerReminderUnpaidRow(string PartnerName, decimal AmountDue, bool HasLinkedUser);

public interface IReminderEmailService
{
    Task SendPartnerReminderAsync(PartnerPaymentReminderEmail request, CancellationToken cancellationToken = default);

    Task SendOwnerReminderAsync(OwnerPaymentReminderEmail request, CancellationToken cancellationToken = default);
}
