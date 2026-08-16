using FuzulTaksitTakip.Domain.Common;
using FuzulTaksitTakip.Domain.Enums;

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
    public string? ReceiptStorageKey { get; set; }
    public string? ReceiptContentType { get; set; }
    public string? ReceiptFileName { get; set; }
    public DateTime? ReceiptUploadedAtUtc { get; set; }
    public PaymentReviewStatus ReviewStatus { get; set; } = PaymentReviewStatus.None;
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewedByUserId { get; set; }

    public Installment Installment { get; set; } = null!;
    public Partner Partner { get; set; } = null!;
    public Partner? PaidByPartner { get; set; }
}
