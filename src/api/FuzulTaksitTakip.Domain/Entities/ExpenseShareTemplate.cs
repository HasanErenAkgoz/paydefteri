namespace FuzulTaksitTakip.Domain.Entities;

public class ExpenseShareTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecurrenceId { get; set; }
    public Guid PartnerId { get; set; }
    public decimal Amount { get; set; }

    public ExpenseRecurrence Recurrence { get; set; } = null!;
    public Partner Partner { get; set; } = null!;
}
