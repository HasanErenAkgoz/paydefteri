using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PayDefteri.Application.Common.Interfaces;

namespace PayDefteri.Infrastructure.Email;

public sealed class EmailVerificationService : IEmailVerificationService
{
    private readonly IEmailSender _emailSender;
    private readonly AppOptions _app;
    private readonly ILogger<EmailVerificationService> _logger;

    public EmailVerificationService(
        IEmailSender emailSender,
        IOptions<AppOptions> app,
        ILogger<EmailVerificationService> logger)
    {
        _emailSender = emailSender;
        _app = app.Value;
        _logger = logger;
    }

    public async Task<EmailVerificationResult> SendVerificationAsync(
        EmailVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_app.PublicWebUrl)
            ? "http://localhost:4200"
            : _app.PublicWebUrl.TrimEnd('/');
        var verifyUrl =
            $"{baseUrl}/verify-email?userId={Uri.EscapeDataString(request.UserId)}"
            + $"&token={Uri.EscapeDataString(request.Token)}";
        var name = string.IsNullOrWhiteSpace(request.DisplayName)
            ? "Merhaba"
            : request.DisplayName.Trim();

        var message = new EmailMessage(
            request.ToEmail,
            "PayDefteri — E-posta adresinizi doğrulayın",
            BuildHtml(name, verifyUrl),
            BuildPlainText(name, verifyUrl));

        if (!_emailSender.IsConfigured)
        {
            // Logging sender: the link still reaches the developer console.
            await _emailSender.SendAsync(message, cancellationToken);
            return new EmailVerificationResult(Sent: false, Configured: false);
        }

        try
        {
            await _emailSender.SendAsync(message, cancellationToken);
            return new EmailVerificationResult(Sent: true, Configured: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to {Email}", request.ToEmail);
            return new EmailVerificationResult(Sent: false, Configured: true);
        }
    }

    private static string BuildPlainText(string name, string verifyUrl) =>
        new StringBuilder()
            .AppendLine("PayDefteri — E-posta doğrulama")
            .AppendLine()
            .AppendLine($"{name},")
            .AppendLine()
            .AppendLine("Hesabınızı kullanmaya başlamak için e-posta adresinizi doğrulayın.")
            .AppendLine()
            .AppendLine($"Doğrulama bağlantısı: {verifyUrl}")
            .AppendLine()
            .AppendLine("Bu kaydı siz yapmadıysanız bu e-postayı yok sayabilirsiniz.")
            .ToString();

    private static string BuildHtml(string name, string verifyUrl)
    {
        var eName = WebUtility.HtmlEncode(name);
        var eUrl = WebUtility.HtmlEncode(verifyUrl);

        return $"""
            <!DOCTYPE html>
            <html lang="tr">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <title>E-posta doğrulama</title>
            </head>
            <body style="margin:0;padding:0;background:#f4f5f7;color:#1a1d23;">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background:#f4f5f7;padding:40px 16px;">
                <tr>
                  <td align="center">
                    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="max-width:580px;background:#ffffff;border:1px solid #e5e7eb;">
                      <tr>
                        <td style="padding:28px 36px 20px;border-bottom:3px solid #1e3a5f;font-family:Georgia,'Times New Roman',serif;">
                          <div style="font-size:22px;font-weight:700;color:#1e3a5f;letter-spacing:0.02em;">PayDefteri</div>
                          <div style="margin-top:4px;font-family:Arial,Helvetica,sans-serif;font-size:12px;color:#6b7280;letter-spacing:0.04em;text-transform:uppercase;">Ortak borç ve taksit yönetimi</div>
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:32px 36px 8px;font-family:Arial,Helvetica,sans-serif;font-size:15px;line-height:1.6;color:#1a1d23;">
                          <p style="margin:0 0 16px;">{eName},</p>
                          <p style="margin:0 0 16px;">Hesabınızı kullanmaya başlamak için e-posta adresinizi doğrulayın.</p>
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:8px 36px 32px;font-family:Arial,Helvetica,sans-serif;">
                          <a href="{eUrl}" style="display:inline-block;padding:12px 24px;background:#1e3a5f;color:#ffffff;font-size:15px;font-weight:700;text-decoration:none;">E-postamı doğrula</a>
                          <p style="margin:20px 0 0;font-size:12px;color:#6b7280;word-break:break-all;">{eUrl}</p>
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:16px 36px 28px;border-top:1px solid #e5e7eb;font-family:Arial,Helvetica,sans-serif;font-size:12px;color:#6b7280;">
                          Bu kaydı siz yapmadıysanız bu e-postayı yok sayabilirsiniz.
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }
}
