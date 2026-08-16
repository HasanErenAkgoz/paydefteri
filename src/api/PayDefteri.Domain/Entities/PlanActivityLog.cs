using PayDefteri.Domain.Common;
using PayDefteri.Domain.Enums;

namespace PayDefteri.Domain.Entities;

public class PlanActivityLog : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlanId { get; set; }
    public string ActorUserId { get; set; } = string.Empty;
    public string ActorDisplayName { get; set; } = string.Empty;
    public PlanActivityType Type { get; set; }
    public string Message { get; set; } = string.Empty;

    public Plan Plan { get; set; } = null!;
}
