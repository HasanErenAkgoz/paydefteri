using FuzulTaksitTakip.Domain.Common;
using FuzulTaksitTakip.Domain.Enums;

namespace FuzulTaksitTakip.Domain.Entities;

public class PlanMember : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlanId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public PlanMemberRole Role { get; set; } = PlanMemberRole.Member;
    /// <summary>Which partner row this user may mark payments for.</summary>
    public Guid? PartnerId { get; set; }

    public Plan Plan { get; set; } = null!;
    public Partner? Partner { get; set; }
}
