using PayDefteri.Domain.Common;
using PayDefteri.Domain.Enums;

namespace PayDefteri.Domain.Entities;

public class PaymentReminderLog : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlanId { get; set; }
    public Guid InstallmentId { get; set; }
    /// <summary>Null means owner summary email for the installment.</summary>
    public Guid? PartnerId { get; set; }
    public PaymentReminderKind Kind { get; set; }
    public int OffsetDays { get; set; }
    public DateOnly SentOn { get; set; }

    public Plan Plan { get; set; } = null!;
    public Installment Installment { get; set; } = null!;
}
