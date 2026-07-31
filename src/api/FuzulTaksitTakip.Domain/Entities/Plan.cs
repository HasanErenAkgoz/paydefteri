using FuzulTaksitTakip.Domain.Common;

namespace FuzulTaksitTakip.Domain.Entities;

public class Plan : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OwnerUserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? DeliveryInstallmentId { get; set; }

    public ICollection<Partner> Partners { get; set; } = new List<Partner>();
    public ICollection<Installment> Installments { get; set; } = new List<Installment>();
}
