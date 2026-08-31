using PayDefteri.Domain.Common;

namespace PayDefteri.Domain.Entities;

public sealed class SpendingCategoryBudget : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OwnerUserId { get; set; } = string.Empty;
    public DateOnly Month { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Currency { get; set; } = "TRY";
    public decimal Amount { get; set; }
}
