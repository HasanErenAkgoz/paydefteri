using System.Globalization;
using System.Text;
using PayDefteri.Application.Common.Exceptions;
using PayDefteri.Application.Common.Interfaces;

namespace PayDefteri.Infrastructure.Services;

/// <summary>
/// Provides a deterministic CSV import path. Binary statement formats are deliberately rejected
/// until a configured document-analysis provider is available, rather than silently importing
/// guessed transactions.
/// </summary>
public sealed class GeminiCreditCardStatementAnalyzer : ICreditCardStatementAnalyzer
{
    public Task<CreditCardStatementAnalysis> AnalyzeAsync(CreditCardStatementAnalysisInput input, CancellationToken cancellationToken = default)
    {
        if (input.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) || input.ContentType.Contains("csv", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(ParseCsv(input));
        }

        throw new ExternalServiceUnavailableException("PDF, Excel ve görsel ekstre analizi için belge analiz servisi henüz yapılandırılmamış. CSV ekstresini içe aktarabilir veya sunucuda Gemini yapılandırmasını tamamlayabilirsiniz.");
    }

    private static CreditCardStatementAnalysis ParseCsv(CreditCardStatementAnalysisInput input)
    {
        var text = Encoding.UTF8.GetString(input.Content).TrimStart('\uFEFF');
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length < 2) throw new ExternalServiceUnavailableException("CSV ekstresinde başlık ve en az bir işlem satırı olmalıdır.");

        var delimiter = lines[0].Count(c => c == ';') >= lines[0].Count(c => c == ',') ? ';' : ',';
        var headers = SplitLine(lines[0], delimiter).Select(Normalize).ToArray();
        var dateIndex = FindHeader(headers, "tarih", "date", "işlem tarihi", "islem tarihi");
        var descriptionIndex = FindHeader(headers, "açıklama", "aciklama", "işlem", "islem", "üye işyeri", "uye isyeri", "description");
        var amountIndex = FindHeader(headers, "tutar", "amount", "borç", "borc");
        if (dateIndex < 0 || descriptionIndex < 0 || amountIndex < 0)
            throw new ExternalServiceUnavailableException("CSV başlıklarında tarih, açıklama ve tutar alanları bulunmalıdır.");

        var transactions = new List<CreditCardStatementTransaction>();
        for (var index = 1; index < lines.Length; index++)
        {
            var cells = SplitLine(lines[index], delimiter);
            if (cells.Count <= Math.Max(dateIndex, Math.Max(descriptionIndex, amountIndex))) continue;
            if (!TryParseDate(cells[dateIndex], out var occurredOn) || !TryParseAmount(cells[amountIndex], out var amount) || amount == 0) continue;
            var description = cells[descriptionIndex].Trim();
            if (string.IsNullOrWhiteSpace(description)) continue;
            var isRefund = amount < 0 || description.Contains("iade", StringComparison.OrdinalIgnoreCase);
            transactions.Add(new CreditCardStatementTransaction(occurredOn, description, Math.Abs(amount), isRefund, null, null, null, description, 0.95m, "CSV ekstresinden aktarıldı."));
        }
        if (transactions.Count == 0) throw new ExternalServiceUnavailableException("CSV ekstresinden geçerli işlem okunamadı. Tarih ve tutar biçimini kontrol edin.");
        return new CreditCardStatementAnalysis(null, null, null, null, null, null, transactions, ["CSV işlemleri otomatik okunmuştur; içe aktarmadan önce kategori ve mükerrer kayıtları kontrol edin."]);
    }

    private static int FindHeader(IReadOnlyList<string> headers, params string[] candidates) => Array.FindIndex(headers.ToArray(), header => candidates.Any(candidate => header.Contains(Normalize(candidate), StringComparison.Ordinal)));
    private static string Normalize(string value) => value.Trim().ToLowerInvariant().Replace('ı', 'i').Replace('ş', 's').Replace('ğ', 'g').Replace('ü', 'u').Replace('ö', 'o').Replace('ç', 'c');
    private static List<string> SplitLine(string line, char delimiter) => line.Split(delimiter).Select(cell => cell.Trim().Trim('"')).ToList();
    private static bool TryParseDate(string value, out DateOnly date) => DateOnly.TryParse(value, CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.AllowWhiteSpaces, out date) || DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out date);
    private static bool TryParseAmount(string value, out decimal amount)
    {
        value = value.Trim().Replace("₺", string.Empty).Replace("TRY", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        return decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo("tr-TR"), out amount)
            || decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out amount);
    }
}
