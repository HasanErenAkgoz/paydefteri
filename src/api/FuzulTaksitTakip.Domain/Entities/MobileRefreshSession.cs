using FuzulTaksitTakip.Domain.Common;

namespace FuzulTaksitTakip.Domain.Entities;

public sealed class MobileRefreshSession : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public Guid FamilyId { get; set; } = Guid.NewGuid();
    public string DeviceName { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string AppVersion { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? LastUsedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public Guid? ReplacedBySessionId { get; set; }

    public bool IsActive(DateTime utcNow) =>
        !IsDeleted && RevokedAtUtc is null && ExpiresAtUtc > utcNow;
}
