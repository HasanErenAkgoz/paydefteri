using PayDefteri.Domain.Common;

namespace PayDefteri.Domain.Entities;

public sealed class SpendingTransaction : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StatementId { get; set; }
    public DateOnly OccurredOn { get; set; }
    public string Description { get; set; } = string.Empty;
    public string MerchantName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsRefund { get; set; }
    public string Currency { get; set; } = "TRY";
    public string Category { get; set; } = "Diğer";
    public int? InstallmentCurrent { get; set; }
    public int? InstallmentTotal { get; set; }
    public SpendingStatement Statement { get; set; } = null!;
}
