using System.Globalization;
using System.Net;
using System.Text;
using PayDefteri.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PayDefteri.Infrastructure.Email;

public sealed class ReminderEmailService : IReminderEmailService
{
    private readonly IEmailSender _emailSender;
    private readonly AppOptions _app;
    private readonly ILogger<ReminderEmailService> _logger;

    public ReminderEmailService(
        IEmailSender emailSender,
        IOptions<AppOptions> app,
        ILogger<ReminderEmailService> logger)
    {
        _emailSender = emailSender;
        _app = app.Value;
        _logger = logger;
    }

    public async Task SendPartnerReminderAsync(
        PartnerPaymentReminderEmail request,
        CancellationToken cancellationToken = default)
    {
        var dashboardUrl = DashboardUrl(request.PlanId);
        var dueTr = request.DueDate.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR"));
        var amountTr = request.AmountDue.ToString("N2", CultureInfo.GetCultureInfo("tr-TR"));
        var headline = request.IsOverdue
            ? $"Ödeme gecikti — {request.OffsetDays} gün"
            : request.OffsetDays == 0
                ? "Ödeme günü bugün"
                : $"Ödemeye {request.OffsetDays} gün kaldı";

        var subject = $"PayDefteri — {headline}: {request.InstallmentName}";
        var text = new StringBuilder()
            .AppendLine("PayDefteri — Ödeme hatırlatması")
            .AppendLine()
            .AppendLine($"Merhaba {request.RecipientName},")
            .AppendLine()
            .AppendLine($"“{request.PlanTitle}” planındaki “{request.InstallmentName}” taksiti için hatırlatma.")
            .AppendLine($"Vade: {dueTr}")
            .AppendLine($"Tutar: {amountTr} ₺")
            .AppendLine()
            .AppendLine($"Takibe git: {dashboardUrl}")
            .ToString();

        var html = CorporateShell(
            headline,
            $"""
             <p style="margin:0 0 14px;font-size:15px;line-height:1.65;color:#374151;">
               Merhaba <strong style="color:#111827;">{WebUtility.HtmlEncode(request.RecipientName)}</strong>,
             </p>
             <p style="margin:0 0 18px;font-size:15px;line-height:1.65;color:#374151;">
               <strong style="color:#111827;">{WebUtility.HtmlEncode(request.PlanTitle)}</strong> planındaki
               <strong style="color:#111827;">{WebUtility.HtmlEncode(request.InstallmentName)}</strong> taksiti için
               {(request.IsOverdue ? "gecikme" : "yaklaşan ödeme")} hatırlatması.
             </p>
             {InfoTable(("Plan", request.PlanTitle), ("Taksit", request.InstallmentName), ("Vade", dueTr), ("Tutar", $"{amountTr} ₺"))}
             {Cta(dashboardUrl, "Takibe git")}
             """);

        await SendSafeAsync(new EmailMessage(request.ToEmail, subject, html, text), cancellationToken);
    }

    public async Task SendOwnerReminderAsync(
        OwnerPaymentReminderEmail request,
        CancellationToken cancellationToken = default)
    {
        var dashboardUrl = DashboardUrl(request.PlanId);
        var dueTr = request.DueDate.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR"));
        var headline = request.IsOverdue
            ? $"Özet: gecikmiş taksit ({request.OffsetDays} gün)"
            : request.OffsetDays == 0
                ? "Özet: bugün vadeli taksit"
                : $"Özet: {request.OffsetDays} gün sonra vade";

        var subject = $"PayDefteri — {headline}: {request.InstallmentName}";
        var rowsText = string.Join(
            Environment.NewLine,
            request.UnpaidPartners.Select(u =>
                $"- {u.PartnerName}: {u.AmountDue.ToString("N2", CultureInfo.GetCultureInfo("tr-TR"))} ₺" +
                (u.HasLinkedUser ? "" : " (bağlı kullanıcı yok)")));

        var text = new StringBuilder()
            .AppendLine("PayDefteri — Plan sahibi ödeme özeti")
            .AppendLine()
            .AppendLine($"Plan: {request.PlanTitle}")
            .AppendLine($"Taksit: {request.InstallmentName}")
            .AppendLine($"Vade: {dueTr}")
            .AppendLine()
            .AppendLine("Ödenmemiş paylar:")
            .AppendLine(rowsText)
            .AppendLine()
            .AppendLine($"Takibe git: {dashboardUrl}")
            .ToString();

        var rowsHtml = string.Join("", request.UnpaidPartners.Select(u =>
        {
            var amount = u.AmountDue.ToString("N2", CultureInfo.GetCultureInfo("tr-TR"));
            var note = u.HasLinkedUser ? "" : " <span style=\"color:#9ca3af;\">(bağlı yok)</span>";
            return $"""
                <tr>
                  <td style="padding:10px 12px;border-bottom:1px solid #e5e7eb;font-family:Arial,Helvetica,sans-serif;font-size:14px;color:#111827;">{WebUtility.HtmlEncode(u.PartnerName)}{note}</td>
                  <td style="padding:10px 12px;border-bottom:1px solid #e5e7eb;font-family:Arial,Helvetica,sans-serif;font-size:14px;color:#111827;text-align:right;">{WebUtility.HtmlEncode(amount)} ₺</td>
                </tr>
                """;
        }));

        var html = CorporateShell(
            headline,
            $"""
             <p style="margin:0 0 18px;font-size:15px;line-height:1.65;color:#374151;">
               <strong style="color:#111827;">{WebUtility.HtmlEncode(request.PlanTitle)}</strong> planında
               <strong style="color:#111827;">{WebUtility.HtmlEncode(request.InstallmentName)}</strong>
               için ödenmemiş pay özeti (vade {WebUtility.HtmlEncode(dueTr)}).
             </p>
             {InfoTable(("Plan", request.PlanTitle), ("Taksit", request.InstallmentName), ("Vade", dueTr))}
             <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="margin:16px 0;border:1px solid #e5e7eb;">
               <tr>
                 <td style="padding:10px 12px;background:#f9fafb;border-bottom:1px solid #e5e7eb;font-family:Arial,Helvetica,sans-serif;font-size:11px;font-weight:700;color:#6b7280;text-transform:uppercase;">Ortak</td>
                 <td style="padding:10px 12px;background:#f9fafb;border-bottom:1px solid #e5e7eb;font-family:Arial,Helvetica,sans-serif;font-size:11px;font-weight:700;color:#6b7280;text-transform:uppercase;text-align:right;">Tutar</td>
               </tr>
               {rowsHtml}
             </table>
             {Cta(dashboardUrl, "Takibe git")}
             """);

        await SendSafeAsync(new EmailMessage(request.ToEmail, subject, html, text), cancellationToken);
    }

    private string DashboardUrl(Guid planId)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_app.PublicWebUrl)
            ? "http://localhost:4200"
            : _app.PublicWebUrl.TrimEnd('/');
        return $"{baseUrl}/plans/{planId}/dashboard";
    }

    private async Task SendSafeAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        try
        {
            await _emailSender.SendAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reminder email send failed To={To}", message.To);
            throw;
        }
    }

    private static string InfoTable(params (string Label, string Value)[] rows)
    {
        var sb = new StringBuilder();
        sb.Append("""<table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="border:1px solid #e5e7eb;">""");
        for (var i = 0; i < rows.Length; i++)
        {
            var (label, value) = rows[i];
            var border = i == rows.Length - 1 ? "" : "border-bottom:1px solid #e5e7eb;";
            sb.Append($"""
                <tr>
                  <td style="width:36%;padding:12px 14px;background:#f9fafb;{border}border-right:1px solid #e5e7eb;font-family:Arial,Helvetica,sans-serif;font-size:12px;font-weight:700;color:#6b7280;text-transform:uppercase;letter-spacing:0.04em;">{WebUtility.HtmlEncode(label)}</td>
                  <td style="padding:12px 14px;{border}font-family:Arial,Helvetica,sans-serif;font-size:14px;color:#111827;font-weight:600;">{WebUtility.HtmlEncode(value)}</td>
                </tr>
                """);
        }

        sb.Append("</table>");
        return sb.ToString();
    }

    private static string Cta(string url, string label) =>
        $"""
         <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="margin-top:22px;">
           <tr>
             <td style="background:#1e3a5f;">
               <a href="{WebUtility.HtmlEncode(url)}" style="display:inline-block;padding:13px 26px;font-family:Arial,Helvetica,sans-serif;font-size:14px;font-weight:700;color:#ffffff;text-decoration:none;">{WebUtility.HtmlEncode(label)}</a>
             </td>
           </tr>
         </table>
         """;

    private static string CorporateShell(string title, string bodyInner) =>
        $"""
         <!DOCTYPE html>
         <html lang="tr">
         <head><meta charset="utf-8" /><meta name="viewport" content="width=device-width, initial-scale=1" /><title>{WebUtility.HtmlEncode(title)}</title></head>
         <body style="margin:0;padding:0;background:#f4f5f7;color:#1a1d23;">
           <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background:#f4f5f7;padding:40px 16px;">
             <tr><td align="center">
               <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="max-width:580px;background:#ffffff;border:1px solid #e5e7eb;">
                 <tr>
                   <td style="padding:28px 36px 20px;border-bottom:3px solid #1e3a5f;font-family:Georgia,'Times New Roman',serif;">
                     <div style="font-size:22px;font-weight:700;color:#1e3a5f;">PayDefteri</div>
                     <div style="margin-top:4px;font-family:Arial,Helvetica,sans-serif;font-size:12px;color:#6b7280;letter-spacing:0.04em;text-transform:uppercase;">Ödeme hatırlatması</div>
                   </td>
                 </tr>
                 <tr>
                   <td style="padding:28px 36px;font-family:Arial,Helvetica,sans-serif;">
                     <h1 style="margin:0 0 16px;font-size:20px;line-height:1.35;font-weight:700;color:#111827;">{WebUtility.HtmlEncode(title)}</h1>
                     {bodyInner}
                   </td>
                 </tr>
                 <tr>
                   <td style="padding:18px 36px 26px;border-top:1px solid #e5e7eb;background:#fafafa;font-family:Arial,Helvetica,sans-serif;font-size:11px;color:#9ca3af;">
                     PayDefteri · Bu bir bilgilendirme e-postasıdır.
                   </td>
                 </tr>
               </table>
             </td></tr>
           </table>
         </body>
         </html>
         """;
}
