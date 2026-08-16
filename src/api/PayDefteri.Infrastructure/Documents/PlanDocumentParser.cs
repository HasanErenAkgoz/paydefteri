using System.Text;
using ClosedXML.Excel;
using PayDefteri.Application.Common.Interfaces;
using PayDefteri.Domain.Services;
using UglyToad.PdfPig;

namespace PayDefteri.Infrastructure.Documents;

public sealed class PlanDocumentParser : IPlanDocumentParser
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".xlsx", ".xls", ".csv"
    };

    private const long MaxBytes = 12 * 1024 * 1024;

    public Task<ParsedPlanDocument> ParseAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var ext = Path.GetExtension(fileName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
        {
            throw new InvalidOperationException("Desteklenen dosyalar: PDF, Excel (.xlsx) veya CSV.");
        }

        if (content.CanSeek && content.Length > MaxBytes)
        {
            throw new InvalidOperationException("Dosya en fazla 12 MB olabilir.");
        }

        using var buffer = new MemoryStream();
        content.CopyTo(buffer);
        if (buffer.Length > MaxBytes)
        {
            throw new InvalidOperationException("Dosya en fazla 12 MB olabilir.");
        }

        buffer.Position = 0;
        var kind = ext.TrimStart('.').ToUpperInvariant();

        var (title, description, deliveryIndex, rows, warnings) = ext.ToLowerInvariant() switch
        {
            ".pdf" => ParsePdf(buffer),
            ".csv" => ParseCsv(buffer, Path.GetFileNameWithoutExtension(fileName)),
            _ => ParseExcel(buffer, Path.GetFileNameWithoutExtension(fileName))
        };

        if (rows.Count == 0)
        {
            throw new InvalidOperationException(
                warnings.FirstOrDefault()
                ?? "Dosyadan taksit satırı çıkarılamadı. Tablo metni içeren bir ödeme planı yükleyin.");
        }

        var partners = ScheduleTableParser.DefaultPartners()
            .Select(p => new ParsedPartner(p.Name, p.Color, p.DefaultPct))
            .ToList();

        var installments = rows
            .Select((r, i) => new ParsedInstallment(i + 1, r.Name, r.Due, r.Amount))
            .ToList();

        if (deliveryIndex < 0 || deliveryIndex >= installments.Count)
        {
            deliveryIndex = -1;
        }

        return Task.FromResult(new ParsedPlanDocument(
            title,
            description,
            Path.GetFileName(fileName) ?? "document",
            kind,
            deliveryIndex,
            partners,
            installments,
            warnings));
    }

    private static (string Title, string Description, int DeliveryIndex, List<(string Name, DateOnly Due, decimal Amount)> Rows, List<string> Warnings)
        ParsePdf(Stream stream)
    {
        using var doc = PdfDocument.Open(stream);
        var sb = new StringBuilder();
        foreach (var page in doc.GetPages())
        {
            // PdfPig page.Text often concatenates tokens without spaces on Fuzul PDFs.
            // Word stream preserves separators the table regex needs.
            var words = page.GetWords().Select(w => w.Text);
            sb.AppendLine(string.Join(' ', words));
            sb.AppendLine();
        }

        return ScheduleTableParser.ParseFromPlainText(sb.ToString());
    }

    private static (string Title, string Description, int DeliveryIndex, List<(string Name, DateOnly Due, decimal Amount)> Rows, List<string> Warnings)
        ParseExcel(Stream stream, string? fallbackTitle)
    {
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();
        var text = SheetToPseudoText(sheet);
        var parsed = ScheduleTableParser.ParseFromPlainText(text, fallbackTitle);

        if (parsed.Rows.Count > 0)
        {
            return parsed;
        }

        // Fallback: scan cells for date + amount columns
        var gridRows = ParseExcelGrid(sheet);
        if (gridRows.Count == 0)
        {
            return parsed;
        }

        var merged = ScheduleTableParser.MergeSameDay(gridRows);
        return (
            fallbackTitle ?? "Excel ödeme planı",
            $"Excel'den aktarıldı (≈ {merged.Sum(r => r.Amount):N2} ₺)",
            InferDeliveryFromNames(merged),
            merged,
            parsed.Warnings);
    }

    private static (string Title, string Description, int DeliveryIndex, List<(string Name, DateOnly Due, decimal Amount)> Rows, List<string> Warnings)
        ParseCsv(Stream stream, string? fallbackTitle)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var text = reader.ReadToEnd();
        // Normalize CSV separators into spaces so regex can catch lines
        var normalized = text
            .Replace(';', ' ')
            .Replace(',', ' ');
        return ScheduleTableParser.ParseFromPlainText(normalized, fallbackTitle);
    }

    private static string SheetToPseudoText(IXLWorksheet sheet)
    {
        var sb = new StringBuilder();
        var range = sheet.RangeUsed();
        if (range is null)
        {
            return string.Empty;
        }

        foreach (var row in range.RowsUsed())
        {
            var cells = row.CellsUsed().Select(c => c.GetFormattedString().Trim()).Where(s => s.Length > 0);
            sb.AppendLine(string.Join(" ", cells));
        }

        return sb.ToString();
    }

    private static List<(string Name, DateOnly Due, decimal Amount)> ParseExcelGrid(IXLWorksheet sheet)
    {
        var result = new List<(string Name, DateOnly Due, decimal Amount)>();
        var range = sheet.RangeUsed();
        if (range is null)
        {
            return result;
        }

        foreach (var row in range.RowsUsed().Skip(1))
        {
            string? name = null;
            DateOnly? due = null;
            decimal? amount = null;

            foreach (var cell in row.CellsUsed())
            {
                if (cell.DataType == XLDataType.DateTime)
                {
                    var dt = cell.GetDateTime();
                    due = DateOnly.FromDateTime(dt);
                    continue;
                }

                var raw = cell.GetFormattedString().Trim();
                if (ScheduleTableParser.TryParseTrDate(raw, out var d))
                {
                    due = d;
                    continue;
                }

                if (cell.DataType == XLDataType.Number)
                {
                    amount = cell.GetValue<decimal>();
                    continue;
                }

                if (ScheduleTableParser.TryParseTrMoney(raw, out var money))
                {
                    amount = money;
                    continue;
                }

                if (name is null && raw.Length > 1 && !raw.All(char.IsDigit))
                {
                    name = raw;
                }
            }

            if (due is not null && amount is not null && amount >= 0)
            {
                result.Add((name ?? $"{result.Count + 1}. Taksit", due.Value, amount.Value));
            }
        }

        return result;
    }

    private static int InferDeliveryFromNames(IReadOnlyList<(string Name, DateOnly Due, decimal Amount)> rows)
    {
        for (var i = 0; i < rows.Count; i++)
        {
            if (rows[i].Name.Contains("Tahsisat", StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }
}
