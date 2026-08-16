namespace PayDefteri.Domain.Entities;

public class InstallmentShare
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InstallmentId { get; set; }
    public Guid PartnerId { get; set; }
    public decimal Amount { get; set; }

    public Installment Installment { get; set; } = null!;
    public Partner Partner { get; set; } = null!;
}
