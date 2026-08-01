using FuzulTaksitTakip.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace FuzulTaksitTakip.Infrastructure.Email;

/// <summary>Dev/fallback transport: writes the message to logs instead of SMTP.</summary>
public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public bool IsConfigured => false;

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Email (not sent — SMTP disabled). To={To} Subject={Subject}\n{Body}",
            message.To,
            message.Subject,
            message.TextBody ?? message.HtmlBody);
        return Task.CompletedTask;
    }
}
