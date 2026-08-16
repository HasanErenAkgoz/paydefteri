using PayDefteri.Domain.Common;

namespace PayDefteri.Domain.Entities;

public class ExpenseCategory : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#94a3b8";
    public int SortOrder { get; set; }

    public Plan Plan { get; set; } = null!;
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
