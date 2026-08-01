using FuzulTaksitTakip.Domain.Common;
using FuzulTaksitTakip.Domain.Enums;

namespace FuzulTaksitTakip.Domain.Entities;

public class Plan : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OwnerUserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? DeliveryInstallmentId { get; set; }
    public bool RequireReceipt { get; set; }
    public IbanMode IbanMode { get; set; } = IbanMode.None;
    public string? SettlementIban { get; set; }
    public bool RemindersEnabled { get; set; }
    public int[] ReminderDaysBefore { get; set; } = Array.Empty<int>();
    public int[] ReminderDaysAfter { get; set; } = Array.Empty<int>();

    public ICollection<Partner> Partners { get; set; } = new List<Partner>();
    public ICollection<Installment> Installments { get; set; } = new List<Installment>();
    public ICollection<PlanMember> Members { get; set; } = new List<PlanMember>();
    public ICollection<PlanInvite> Invites { get; set; } = new List<PlanInvite>();
    public ICollection<PaymentReminderLog> ReminderLogs { get; set; } = new List<PaymentReminderLog>();
    public ICollection<PlanActivityLog> ActivityLogs { get; set; } = new List<PlanActivityLog>();
}
