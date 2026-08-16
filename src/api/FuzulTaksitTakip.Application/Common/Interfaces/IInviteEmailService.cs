namespace FuzulTaksitTakip.Application.Common.Interfaces;

public sealed record InviteEmailRequest(
    string ToEmail,
    string PlanTitle,
    string PartnerName,
    string InviterDisplayName,
    string InviteToken,
    DateTime ExpiresAtUtc);

public sealed record InviteEmailResult(bool Sent, bool Configured);

public interface IInviteEmailService
{
    Task<InviteEmailResult> SendInviteAsync(InviteEmailRequest request, CancellationToken cancellationToken = default);
}
