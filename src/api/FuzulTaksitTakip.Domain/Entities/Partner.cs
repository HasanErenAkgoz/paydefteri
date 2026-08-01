using FuzulTaksitTakip.Domain.Common;

namespace FuzulTaksitTakip.Domain.Entities;

public class Partner : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#38bdf8";
    public decimal DefaultPct { get; set; }
    public int SortOrder { get; set; }
    /// <summary>App user linked to this partner for self-service payment marking.</summary>
    public string? LinkedUserId { get; set; }
    /// <summary>Used when plan IbanMode is Partner.</summary>
    public string? Iban { get; set; }

    public Plan Plan { get; set; } = null!;
}
