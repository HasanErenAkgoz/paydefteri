namespace FuzulTaksitTakip.Application.Common.Interfaces;

public sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string? TextBody = null);

public interface IEmailSender
{
    /// <summary>True when SMTP (or another transport) is configured and enabled.</summary>
    bool IsConfigured { get; }

    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
