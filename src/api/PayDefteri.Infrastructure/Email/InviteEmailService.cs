using System.Net;
using System.Text;
using PayDefteri.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PayDefteri.Infrastructure.Email;

public sealed class InviteEmailService : IInviteEmailService
{
    private readonly IEmailSender _emailSender;
    private readonly AppOptions _app;
    private readonly ILogger<InviteEmailService> _logger;

    public InviteEmailService(
        IEmailSender emailSender,
        IOptions<AppOptions> app,
        ILogger<InviteEmailService> logger)
    {
        _emailSender = emailSender;
        _app = app.Value;
        _logger = logger;
    }

    public async Task<InviteEmailResult> SendInviteAsync(
        InviteEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_app.PublicWebUrl)
            ? "http://localhost:4200"
            : _app.PublicWebUrl.TrimEnd('/');
        var inviteUrl = $"{baseUrl}/invite/{Uri.EscapeDataString(request.InviteToken)}";
        var expiresTr = request.ExpiresAtUtc.ToLocalTime().ToString("dd.MM.yyyy");
        var inviter = string.IsNullOrWhiteSpace(request.InviterDisplayName)
            ? "Bir kullanıcı"
            : request.InviterDisplayName.Trim();

        var subject = $"PayDefteri — “{request.PlanTitle}” planına davet";
        var text = BuildPlainText(inviter, request, inviteUrl, expiresTr);
        var html = BuildHtml(inviter, request, inviteUrl, expiresTr);

        var message = new EmailMessage(request.ToEmail, subject, html, text);

        if (!_emailSender.IsConfigured)
        {
            await _emailSender.SendAsync(message, cancellationToken);
            return new InviteEmailResult(Sent: false, Configured: false);
        }

        try
        {
            await _emailSender.SendAsync(message, cancellationToken);
            return new InviteEmailResult(Sent: true, Configured: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send invite email to {Email}", request.ToEmail);
            return new InviteEmailResult(Sent: false, Configured: true);
        }
    }

    private static string BuildPlainText(
        string inviter,
        InviteEmailRequest request,
        string inviteUrl,
        string expiresTr)
    {
        return new StringBuilder()
            .AppendLine("PayDefteri — Plan daveti")
            .AppendLine()
            .AppendLine("Merhaba,")
            .AppendLine()
            .AppendLine($"{inviter}, sizi “{request.PlanTitle}” planına davet etti.")
            .AppendLine()
            .AppendLine("Bu e-posta doğrudan size gönderilmiştir.")
            .AppendLine(
                $"Daveti kabul ettiğinizde plandaki “{request.PartnerName}” payına bağlanırsınız; yalnızca bu payın ödemelerini işaretleyebilirsiniz.")
            .AppendLine()
            .AppendLine($"Daveti kabul etmek için: {inviteUrl}")
            .AppendLine()
            .AppendLine($"Son geçerlilik tarihi: {expiresTr}")
            .AppendLine("Bu daveti beklemiyorsanız yok sayabilirsiniz.")
            .ToString();
    }

    private static string BuildHtml(
        string inviter,
        InviteEmailRequest request,
        string inviteUrl,
        string expiresTr)
    {
        var eInviter = WebUtility.HtmlEncode(inviter);
        var ePlan = WebUtility.HtmlEncode(request.PlanTitle);
        var ePartner = WebUtility.HtmlEncode(request.PartnerName);
        var eUrl = WebUtility.HtmlEncode(inviteUrl);
        var eExpires = WebUtility.HtmlEncode(expiresTr);
        var eTo = WebUtility.HtmlEncode(request.ToEmail);

        // Corporate / fintech-style transactional email (table layout for clients).
        return $"""
            <!DOCTYPE html>
            <html lang="tr">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <title>Plan daveti</title>
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
                        <td style="padding:32px 36px 8px;font-family:Arial,Helvetica,sans-serif;">
                          <p style="margin:0 0 6px;font-size:12px;font-weight:700;letter-spacing:0.08em;text-transform:uppercase;color:#6b7280;">Davetiye</p>
                          <h1 style="margin:0 0 16px;font-size:22px;line-height:1.35;font-weight:700;color:#111827;font-family:Arial,Helvetica,sans-serif;">Ödeme planı daveti</h1>
                          <p style="margin:0;font-size:15px;line-height:1.65;color:#374151;">
                            Merhaba,<br/><br/>
                            <strong style="color:#111827;">{eInviter}</strong>, sizi
                            <strong style="color:#111827;">“{ePlan}”</strong> planına davet etti.
                          </p>
                          <p style="margin:14px 0 0;font-size:15px;line-height:1.65;color:#374151;">
                            Bu e-posta <strong style="color:#111827;">{eTo}</strong> adresine gönderilmiştir.
                            Daveti kabul ettiğinizde plandaki
                            <strong style="color:#111827;">“{ePartner}”</strong> payına bağlanırsınız;
                            yalnızca bu payın ödemelerini işaretleyebilirsiniz.
                          </p>
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:24px 36px;">
                          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="border:1px solid #e5e7eb;">
                            <tr>
                              <td style="width:40%;padding:14px 16px;background:#f9fafb;border-bottom:1px solid #e5e7eb;border-right:1px solid #e5e7eb;font-family:Arial,Helvetica,sans-serif;font-size:12px;font-weight:700;color:#6b7280;text-transform:uppercase;letter-spacing:0.04em;">Davet eden</td>
                              <td style="padding:14px 16px;border-bottom:1px solid #e5e7eb;font-family:Arial,Helvetica,sans-serif;font-size:14px;color:#111827;font-weight:600;">{eInviter}</td>
                            </tr>
                            <tr>
                              <td style="width:40%;padding:14px 16px;background:#f9fafb;border-bottom:1px solid #e5e7eb;border-right:1px solid #e5e7eb;font-family:Arial,Helvetica,sans-serif;font-size:12px;font-weight:700;color:#6b7280;text-transform:uppercase;letter-spacing:0.04em;">Plan adı</td>
                              <td style="padding:14px 16px;border-bottom:1px solid #e5e7eb;font-family:Arial,Helvetica,sans-serif;font-size:14px;color:#111827;font-weight:600;">{ePlan}</td>
                            </tr>
                            <tr>
                              <td style="width:40%;padding:14px 16px;background:#f9fafb;border-bottom:1px solid #e5e7eb;border-right:1px solid #e5e7eb;font-family:Arial,Helvetica,sans-serif;font-size:12px;font-weight:700;color:#6b7280;text-transform:uppercase;letter-spacing:0.04em;">Alıcı e-posta</td>
                              <td style="padding:14px 16px;border-bottom:1px solid #e5e7eb;font-family:Arial,Helvetica,sans-serif;font-size:14px;color:#111827;font-weight:600;">{eTo}</td>
                            </tr>
                            <tr>
                              <td style="width:40%;padding:14px 16px;background:#f9fafb;border-bottom:1px solid #e5e7eb;border-right:1px solid #e5e7eb;font-family:Arial,Helvetica,sans-serif;font-size:12px;font-weight:700;color:#6b7280;text-transform:uppercase;letter-spacing:0.04em;">Bağlanacağınız pay</td>
                              <td style="padding:14px 16px;border-bottom:1px solid #e5e7eb;font-family:Arial,Helvetica,sans-serif;font-size:14px;color:#111827;font-weight:600;">{ePartner}</td>
                            </tr>
                            <tr>
                              <td style="width:40%;padding:14px 16px;background:#f9fafb;border-right:1px solid #e5e7eb;font-family:Arial,Helvetica,sans-serif;font-size:12px;font-weight:700;color:#6b7280;text-transform:uppercase;letter-spacing:0.04em;">Son geçerlilik</td>
                              <td style="padding:14px 16px;font-family:Arial,Helvetica,sans-serif;font-size:14px;color:#111827;font-weight:600;">{eExpires}</td>
                            </tr>
                          </table>
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:4px 36px 28px;font-family:Arial,Helvetica,sans-serif;">
                          <p style="margin:0 0 18px;font-size:14px;line-height:1.6;color:#4b5563;">
                            Planı görmek ve kendi payınızın ödemelerini takip etmek için daveti kabul edin.
                          </p>
                          <table role="presentation" cellpadding="0" cellspacing="0" border="0">
                            <tr>
                              <td style="background:#1e3a5f;">
                                <a href="{eUrl}" style="display:inline-block;padding:13px 26px;font-family:Arial,Helvetica,sans-serif;font-size:14px;font-weight:700;color:#ffffff;text-decoration:none;letter-spacing:0.02em;">
                                  Daveti kabul et
                                </a>
                              </td>
                            </tr>
                          </table>
                          <p style="margin:20px 0 0;font-size:12px;line-height:1.55;color:#6b7280;word-break:break-all;">
                            Bağlantı çalışmazsa tarayıcınıza yapıştırın:<br/>
                            <a href="{eUrl}" style="color:#1e3a5f;text-decoration:underline;">{eUrl}</a>
                          </p>
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:20px 36px 28px;border-top:1px solid #e5e7eb;background:#fafafa;font-family:Arial,Helvetica,sans-serif;">
                          <p style="margin:0;font-size:12px;line-height:1.6;color:#6b7280;">
                            Bu ileti yalnızca <strong style="color:#374151;">{eTo}</strong> adresine gönderilmiştir.
                            Daveti beklemiyorsanız yok sayabilirsiniz; hesabınızda değişiklik olmaz.
                          </p>
                          <p style="margin:14px 0 0;font-size:11px;color:#9ca3af;">
                            PayDefteri · Bu bir bilgilendirme e-postasıdır.
                          </p>
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
