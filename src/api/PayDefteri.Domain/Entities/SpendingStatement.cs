using PayDefteri.Domain.Common;

namespace PayDefteri.Domain.Entities;

public sealed class SpendingStatement : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OwnerUserId { get; set; } = string.Empty;
    public string SourceHash { get; set; } = string.Empty;
    public string SourceFileName { get; set; } = string.Empty;
    public string SourceKind { get; set; } = string.Empty;
    public DateOnly? PeriodStart { get; set; }
    public DateOnly? PeriodEnd { get; set; }
    public List<SpendingTransaction> Transactions { get; set; } = [];
}
