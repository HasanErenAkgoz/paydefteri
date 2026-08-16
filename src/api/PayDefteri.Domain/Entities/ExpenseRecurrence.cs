using PayDefteri.Domain.Common;
using PayDefteri.Domain.Enums;

namespace PayDefteri.Domain.Entities;

public class ExpenseRecurrence : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlanId { get; set; }
    public Guid? CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public ShareType ShareType { get; set; } = ShareType.Equal;
    public Guid? DefaultPaidByPartnerId { get; set; }
    public RecurrenceFrequency Frequency { get; set; } = RecurrenceFrequency.Monthly;
    /// <summary>1–28 for monthly/yearly day-of-month; 0–6 (Sun–Sat) for weekly.</summary>
    public int AnchorDay { get; set; } = 1;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateOnly NextOccurrence { get; set; }
    public bool IsActive { get; set; } = true;
    public string Note { get; set; } = string.Empty;

    public Plan Plan { get; set; } = null!;
    public ExpenseCategory? Category { get; set; }
    public Partner? DefaultPaidByPartner { get; set; }
    public ICollection<ExpenseShareTemplate> CustomShares { get; set; } = new List<ExpenseShareTemplate>();
    public ICollection<Expense> GeneratedExpenses { get; set; } = new List<Expense>();
}
