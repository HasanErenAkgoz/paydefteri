using FuzulTaksitTakip.Domain.Common;

namespace FuzulTaksitTakip.Domain.Entities;

/// <summary>
/// Cash/bank transfer that settles net debt: FromPartner pays ToPartner.
/// Balance impact: credit From (+), debit To (−) — same sign convention as pay-on-behalf.
/// </summary>
public class SettlementTransfer : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlanId { get; set; }
    public Guid FromPartnerId { get; set; }
    public Guid ToPartnerId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly TransferredOn { get; set; }
    public string Note { get; set; } = string.Empty;

    public Plan Plan { get; set; } = null!;
    public Partner FromPartner { get; set; } = null!;
    public Partner ToPartner { get; set; } = null!;
}
