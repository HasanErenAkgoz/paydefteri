namespace PayDefteri.Application.Common.Interfaces;

public sealed record EmailVerificationRequest(
    string ToEmail,
    string DisplayName,
    string UserId,
    string Token);

public sealed record EmailVerificationResult(bool Sent, bool Configured);

public interface IEmailVerificationService
{
    Task<EmailVerificationResult> SendVerificationAsync(
        EmailVerificationRequest request,
        CancellationToken cancellationToken = default);
}
