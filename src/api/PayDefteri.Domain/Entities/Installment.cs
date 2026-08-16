using PayDefteri.Domain.Common;
using PayDefteri.Domain.Enums;

namespace PayDefteri.Domain.Entities;

public class Installment : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public ShareType ShareType { get; set; } = ShareType.Default;
    public int SortOrder { get; set; }

    public Plan Plan { get; set; } = null!;
    public ICollection<InstallmentShare> CustomShares { get; set; } = new List<InstallmentShare>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
