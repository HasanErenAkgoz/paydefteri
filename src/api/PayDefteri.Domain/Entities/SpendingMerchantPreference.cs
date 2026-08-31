using PayDefteri.Domain.Common;

namespace PayDefteri.Domain.Entities;

public sealed class SpendingMerchantPreference : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OwnerUserId { get; set; } = string.Empty;
    public string MerchantKey { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}
