using FuzulTaksitTakip.Application.Common.Models;
using FuzulTaksitTakip.Domain.Entities;

namespace FuzulTaksitTakip.Application.Common.Mapping;

public static class PlanMappings
{
    public static PlanDto ToDto(this Plan plan) => new(
        plan.Id,
        plan.PlanType,
        plan.Title,
        plan.Description,
        plan.DeliveryInstallmentId,
        plan.CreatedAtUtc,
        plan.RequireReceipt,
        plan.IbanMode,
        plan.SettlementIban,
        plan.RemindersEnabled,
        plan.ReminderDaysBefore ?? Array.Empty<int>(),
        plan.ReminderDaysAfter ?? Array.Empty<int>(),
        plan.IsDeleted);

    public static PartnerDto ToDto(this Partner partner) => new(
        partner.Id,
        partner.PlanId,
        partner.Name,
        partner.Color,
        partner.DefaultPct,
        partner.SortOrder,
        partner.LinkedUserId,
        partner.Iban,
        partner.InviteEmail);

    public static PaymentDto ToDto(this Payment payment) => new(
        payment.PartnerId,
        payment.IsPaid,
        payment.PaidAt,
        payment.PaidByPartnerId,
        payment.Note,
        !string.IsNullOrEmpty(payment.ReceiptStorageKey),
        payment.ReviewStatus);
}
