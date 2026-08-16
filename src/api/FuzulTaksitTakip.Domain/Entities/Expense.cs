using FuzulTaksitTakip.Domain.Common;
using FuzulTaksitTakip.Domain.Enums;

namespace FuzulTaksitTakip.Domain.Entities;

public class Expense : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlanId { get; set; }
    /// <summary>User who manually created this expense. System-generated recurring expenses have no editor.</summary>
    public string? CreatedByUserId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? RecurrenceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly OccurredOn { get; set; }
    public decimal TotalAmount { get; set; }
    public ShareType ShareType { get; set; } = ShareType.Equal;
    public ExpenseStatus Status { get; set; } = ExpenseStatus.Paid;
    /// <summary>
    /// Legacy single payer (full bill). Prefer <see cref="Payments"/>.
    /// Kept for migration/compat; synced when there is exactly one payment.
    /// </summary>
    public Guid? PaidByPartnerId { get; set; }
    public string Note { get; set; } = string.Empty;
    /// <summary>Stable key for recurrence period, e.g. 2026-08 or 2026-W32.</summary>
    public string? PeriodKey { get; set; }

    public Plan Plan { get; set; } = null!;
    public ExpenseCategory? Category { get; set; }
    public ExpenseRecurrence? Recurrence { get; set; }
    public Partner? PaidByPartner { get; set; }
    public ICollection<ExpenseShare> CustomShares { get; set; } = new List<ExpenseShare>();
    public ICollection<ExpensePayment> Payments { get; set; } = new List<ExpensePayment>();
}
