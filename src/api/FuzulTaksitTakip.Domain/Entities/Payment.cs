using FuzulTaksitTakip.Domain.Common;

namespace FuzulTaksitTakip.Domain.Entities;

public class Payment : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InstallmentId { get; set; }
    public Guid PartnerId { get; set; }
    public bool IsPaid { get; set; }
    public DateOnly? PaidAt { get; set; }
    public Guid? PaidByPartnerId { get; set; }
    public string Note { get; set; } = string.Empty;

    public Installment Installment { get; set; } = null!;
    public Partner Partner { get; set; } = null!;
    public Partner? PaidByPartner { get; set; }
}
