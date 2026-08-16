using System.Net;
using System.Net.Mail;
using PayDefteri.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PayDefteri.Infrastructure.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured =>
        _options.Enabled
        && !string.IsNullOrWhiteSpace(_options.Smtp.Host)
        && !string.IsNullOrWhiteSpace(_options.FromAddress);

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Email SMTP is not configured.");
        }

        using var mail = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = message.Subject,
            Body = message.HtmlBody,
            IsBodyHtml = true
        };
        mail.To.Add(message.To);

        #pragma warning disable SYSLIB0014 // SmtpClient is acceptable for basic transactional SMTP
        using var client = new SmtpClient(_options.Smtp.Host, _options.Smtp.Port)
        {
            EnableSsl = _options.Smtp.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };
#pragma warning restore SYSLIB0014

        if (!string.IsNullOrWhiteSpace(_options.Smtp.UserName))
        {
            client.Credentials = new NetworkCredential(_options.Smtp.UserName, _options.Smtp.Password);
        }

        _logger.LogInformation("Sending email to {To} subject {Subject}", message.To, message.Subject);
        await client.SendMailAsync(mail, cancellationToken);
    }
}
