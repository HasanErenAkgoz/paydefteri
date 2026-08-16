namespace PayDefteri.Domain.Entities;

public class ExpenseShare
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExpenseId { get; set; }
    public Guid PartnerId { get; set; }
    public decimal Amount { get; set; }

    public Expense Expense { get; set; } = null!;
    public Partner Partner { get; set; } = null!;
}
