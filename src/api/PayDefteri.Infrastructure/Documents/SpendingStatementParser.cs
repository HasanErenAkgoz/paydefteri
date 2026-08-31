using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using PayDefteri.Application.Common;
using PayDefteri.Application.Common.Interfaces;
using PayDefteri.Domain.Templates;
using UglyToad.PdfPig;

namespace PayDefteri.Infrastructure.Documents;

public sealed partial class SpendingStatementParser : ISpendingStatementParser
{
    private const int MaxRows = 10_000;
    private const int MaxColumns = 100;
    private const int MaxPdfPages = 200;
    private const int MaxWorkbookEntries = 2_000;
    private const long MaxWorkbookExpandedBytes = 64L * 1024 * 1024;
    public Task<ParsedSpendingStatement> ParseAsync(
        byte[] content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = new MemoryStream(content, writable: false);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var rows = extension switch
        {
            ".csv" => ReadCsv(stream, cancellationToken),
            ".xlsx" => ReadWorkbook(stream, cancellationToken),
            ".pdf" => ReadPdf(stream, cancellationToken),
            _ => throw new InvalidDataException("Desteklenen ekstre biçimleri CSV, XLSX ve metin tabanlı PDF'dir."),
        };

        var (transactions, warnings) = ParseRows(rows);
        if (transactions.Count == 0)
        {
            throw new InvalidDataException("Ekstrede tarih, açıklama ve tutar içeren geçerli bir işlem bulunamadı.");
        }

        return Task.FromResult(new ParsedSpendingStatement(
            extension.TrimStart('.').ToUpperInvariant(),
            transactions,
            warnings));
    }

    private static List<List<string>> ReadCsv(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(
            stream,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true),
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: true);
        var lines = new List<string>();
        while (reader.ReadLine() is { } line)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.IsNullOrWhiteSpace(line)) lines.Add(line);
            if (lines.Count > MaxRows) throw new InvalidDataException("Ekstre en fazla 10.000 satır içerebilir.");
        }
        if (lines.Count == 0) return [];

        var delimiter = new[] { ';', ',', '\t' }
            .OrderByDescending(candidate => CountOutsideQuotes(lines[0], candidate))
            .First();
        return lines.Select(line => SplitDelimited(line, delimiter)).ToList();
    }

    private static List<List<string>> ReadWorkbook(Stream stream, CancellationToken cancellationToken)
    {
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true))
        {
            if (archive.Entries.Count > MaxWorkbookEntries
                || archive.Entries.Sum(entry => entry.Length) > MaxWorkbookExpandedBytes)
            {
                throw new InvalidDataException("Excel dosyasının açılmış içeriği güvenli sınırı aşıyor.");
            }
        }
        stream.Position = 0;
        cancellationToken.ThrowIfCancellationRequested();
        using var workbook = new XLWorkbook(stream);
        var range = workbook.Worksheets.FirstOrDefault()?.RangeUsed();
        if (range is null) return [];
        if (range.RowCount() > MaxRows) throw new InvalidDataException("Ekstre en fazla 10.000 satır içerebilir.");
        if (range.ColumnCount() > MaxColumns) throw new InvalidDataException("Ekstre en fazla 100 sütun içerebilir.");
        var rows = new List<List<string>>();
        foreach (var row in range.RowsUsed())
        {
            cancellationToken.ThrowIfCancellationRequested();
            rows.Add(row.Cells(1, range.ColumnCount()).Select(cell => cell.GetFormattedString().Trim()).ToList());
        }
        return rows;
    }

    private static List<List<string>> ReadPdf(Stream stream, CancellationToken cancellationToken)
    {
        var rows = new List<List<string>> { new() { "Tarih", "Açıklama", "Tutar", "Para Birimi" } };
        using var document = PdfDocument.Open(stream);
        if (document.NumberOfPages > MaxPdfPages) throw new InvalidDataException("PDF en fazla 200 sayfa içerebilir.");
        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var lines = page.GetWords()
                .GroupBy(word => Math.Round(word.BoundingBox.Bottom, 1))
                .OrderByDescending(group => group.Key)
                .Select(group => string.Join(' ', group.OrderBy(word => word.BoundingBox.Left).Select(word => word.Text)));
            foreach (var line in lines)
            {
                var match = PdfTransactionPattern().Match(line);
                if (match.Success)
                {
                    rows.Add([
                        match.Groups["date"].Value,
                        match.Groups["description"].Value,
                        match.Groups["amount"].Value,
                        match.Groups["currency"].Value,
                    ]);
                    if (rows.Count > MaxRows) throw new InvalidDataException("Ekstre en fazla 10.000 işlem içerebilir.");
                }
            }
        }
        return rows;
    }

    private static (List<ParsedSpendingTransaction> Transactions, List<string> Warnings) ParseRows(
        IReadOnlyList<List<string>> rows)
    {
        if (rows.Count == 0) return ([], []);
        var headerIndex = rows.ToList().FindIndex(row => row.Any(cell => !string.IsNullOrWhiteSpace(cell)));
        if (headerIndex < 0) return ([], []);
        var headers = rows[headerIndex].Select(Fold).ToList();
        var dateIndex = FindHeader(headers, "tarih", "islem tarihi", "transaction date", "date");
        var descriptionIndex = FindHeader(headers, "aciklama", "islem", "islem aciklamasi", "description", "merchant");
        var amountIndex = FindHeader(headers, "tutar", "islem tutari", "amount", "borc");
        var currencyIndex = FindHeader(headers, "para birimi", "doviz", "currency");
        if (dateIndex < 0 || descriptionIndex < 0 || amountIndex < 0)
        {
            throw new InvalidDataException("Ekstrede Tarih, Açıklama ve Tutar sütunları bulunmalıdır.");
        }

        var transactions = new List<ParsedSpendingTransaction>();
        var skipped = 0;
        foreach (var row in rows.Skip(headerIndex + 1))
        {
            if (!TryCell(row, dateIndex, out var dateText)
                || !TryCell(row, descriptionIndex, out var descriptionText)
                || !TryCell(row, amountIndex, out var amountText)
                || !TryDate(dateText, out var occurredOn)
                || !TryAmount(amountText, out var signedAmount)
                || signedAmount == 0)
            {
                if (row.Any(cell => !string.IsNullOrWhiteSpace(cell))) skipped++;
                continue;
            }

            var description = Limit(SensitiveFinancialDataRedactor.Redact(descriptionText), 500);
            var isRefund = signedAmount < 0
                || Fold(description).Contains("iade", StringComparison.Ordinal)
                || Fold(description).Contains("refund", StringComparison.Ordinal);
            var currency = TryCell(row, currencyIndex, out var currencyText)
                ? NormalizeCurrency(currencyText)
                : CurrencyFrom(amountText);
            var installment = InstallmentPattern().Match(description);
            int? current = installment.Success ? int.Parse(installment.Groups[1].Value, CultureInfo.InvariantCulture) : null;
            int? total = installment.Success ? int.Parse(installment.Groups[2].Value, CultureInfo.InvariantCulture) : null;
            var merchant = Limit(InstallmentPattern().Replace(description, string.Empty).Trim(' ', '-', '/'), 200);

            transactions.Add(new ParsedSpendingTransaction(
                occurredOn,
                description,
                string.IsNullOrWhiteSpace(merchant) ? "Bilinmeyen işyeri" : merchant,
                Math.Abs(signedAmount),
                isRefund,
                currency,
                SpendingCategoryCatalog.Categorize(description),
                current,
                total));
            if (transactions.Count > MaxRows) throw new InvalidDataException("Ekstre en fazla 10.000 işlem içerebilir.");
        }

        var warnings = skipped > 0
            ? new List<string> { $"{skipped} satır geçerli tarih/tutar içermediği için aktarılmadı." }
            : [];
        return (transactions, warnings);
    }

    private static int FindHeader(IReadOnlyList<string> headers, params string[] names)
    {
        for (var index = 0; index < headers.Count; index++)
        {
            if (names.Any(name => headers[index] == Fold(name))) return index;
        }
        return -1;
    }

    private static bool TryCell(IReadOnlyList<string> row, int index, out string value)
    {
        value = index >= 0 && index < row.Count ? row[index].Trim() : string.Empty;
        return value.Length > 0;
    }

    private static bool TryDate(string value, out DateOnly result)
    {
        string[] formats = ["dd.MM.yyyy", "d.M.yyyy", "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd-MM-yyyy", "dd.MM.yy", "dd/MM/yy"];
        return DateOnly.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result)
            || DateOnly.TryParse(value, CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.None, out result);
    }

    private static bool TryAmount(string value, out decimal result)
    {
        var clean = CurrencyPattern().Replace(value, string.Empty)
            .Replace("\u00A0", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Trim();
        var negative = clean.StartsWith('(') && clean.EndsWith(')');
        clean = clean.Trim('(', ')');
        var comma = clean.LastIndexOf(',');
        var dot = clean.LastIndexOf('.');
        if (comma >= 0 && dot >= 0)
        {
            var decimalSeparator = comma > dot ? ',' : '.';
            clean = clean.Replace(decimalSeparator == ',' ? "." : ",", string.Empty, StringComparison.Ordinal)
                .Replace(decimalSeparator, '.');
        }
        else if (comma >= 0)
        {
            clean = clean.Replace(".", string.Empty, StringComparison.Ordinal).Replace(',', '.');
        }

        if (!decimal.TryParse(clean, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out result))
        {
            return false;
        }
        if (negative) result = -Math.Abs(result);
        return true;
    }

    private static string NormalizeCurrency(string value)
    {
        var match = CurrencyPattern().Match(value.ToUpperInvariant());
        var currency = match.Success ? match.Value : value.Trim().ToUpperInvariant();
        return currency is "TL" or "₺" or "TRY" ? "TRY" : currency is "€" ? "EUR" : currency is "$" ? "USD" : Limit(currency, 3);
    }

    private static string CurrencyFrom(string amount)
    {
        var match = CurrencyPattern().Match(amount.ToUpperInvariant());
        return match.Success ? NormalizeCurrency(match.Value) : "TRY";
    }

    private static int CountOutsideQuotes(string value, char delimiter)
    {
        var quoted = false;
        var count = 0;
        foreach (var character in value)
        {
            if (character == '"') quoted = !quoted;
            else if (character == delimiter && !quoted) count++;
        }
        return count;
    }

    private static List<string> SplitDelimited(string line, char delimiter)
    {
        var cells = new List<string>();
        var cell = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (quoted && index + 1 < line.Length && line[index + 1] == '"')
                {
                    cell.Append('"');
                    index++;
                }
                else quoted = !quoted;
            }
            else if (character == delimiter && !quoted)
            {
                cells.Add(cell.ToString().Trim());
                cell.Clear();
            }
            else cell.Append(character);
        }
        cells.Add(cell.ToString().Trim());
        return cells;
    }

    private static string Fold(string value)
    {
        var lowered = value.Trim().ToLowerInvariant()
            .Replace('ı', 'i').Replace('ş', 's').Replace('ğ', 'g')
            .Replace('ü', 'u').Replace('ö', 'o').Replace('ç', 'c');
        return WhitespacePattern().Replace(lowered, " ");
    }

    private static string Limit(string value, int length) => value.Length <= length ? value : value[..length];

    [GeneratedRegex(@"(?<date>\d{1,2}[./-]\d{1,2}[./-]\d{2,4})\s+(?<description>.+?)\s+(?<amount>-?[\d.,]+)(?:\s*(?<currency>TRY|TL|USD|EUR|GBP|₺|€|\$))?$")]
    private static partial Regex PdfTransactionPattern();

    [GeneratedRegex(@"\b(\d{1,3})\s*/\s*(\d{1,3})\b")]
    private static partial Regex InstallmentPattern();

    [GeneratedRegex(@"TRY|TL|USD|EUR|GBP|₺|€|\$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CurrencyPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
