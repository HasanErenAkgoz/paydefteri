using FuzulTaksitTakip.Domain.Common;
using FuzulTaksitTakip.Domain.Enums;

namespace FuzulTaksitTakip.Domain.Entities;

public class PlanInvite : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlanId { get; set; }
    public string Email { get; set; } = string.Empty;
    public Guid PartnerId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string InvitedByUserId { get; set; } = string.Empty;
    public PlanInviteStatus Status { get; set; } = PlanInviteStatus.Pending;
    public DateTime ExpiresAtUtc { get; set; }

    public Plan Plan { get; set; } = null!;
    public Partner Partner { get; set; } = null!;
}
