using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FuzulTaksitTakip.Domain.Services;

/// <summary>
/// Heuristic parser for Turkish tasarruf/finansman ödeme planı tables
/// (Fuzul-style PDF text and generic Excel grids).
/// </summary>
public static partial class ScheduleTableParser
{
    private static readonly string[] PartnerColors = ["#38bdf8", "#fb923c", "#a855f7", "#ec4899"];

    public static (string Title, string Description, int DeliveryIndex, List<(string Name, DateOnly Due, decimal Amount)> Rows, List<string> Warnings)
        ParseFromPlainText(string text, string? preferredTitle = null)
    {
        var warnings = new List<string>();
        var raw = new List<(string Name, DateOnly Due, decimal Amount)>();

        foreach (Match m in InstallmentLineRegex().Matches(text))
        {
            var name = CleanupName(m.Groups["name"].Value);
            if (string.IsNullOrWhiteSpace(name) || IsNoiseName(name) || name.Length > 200)
            {
                continue;
            }

            // Skip false positives from marketing numbers (ay too large / name looks like money)
            if (!int.TryParse(m.Groups["ay"].Value, out var ay) || ay is < 1 or > 48)
            {
                continue;
            }

            if (!TryParseTrDate(m.Groups["date"].Value, out var due))
            {
                continue;
            }

            if (!TryParseTrMoney(m.Groups["amount"].Value, out var amount) || amount <= 0)
            {
                continue;
            }

            raw.Add((name, due, amount));
        }

        if (raw.Count == 0)
        {
            warnings.Add("Tabloda taksit satırı bulunamadı. PDF tarama görüntüsü olabilir veya format desteklenmiyor.");
        }

        // Same-day lines (Peşinat + Tasarruf + Org) → tek satır (Fuzul HTML parity)
        var merged = MergeSameDay(raw);

        var title = preferredTitle;
        if (string.IsNullOrWhiteSpace(title))
        {
            title = InferTitle(text) ?? "Dosyadan aktarılan plan";
        }

        var description = InferDescription(text, merged.Sum(r => r.Amount));
        var deliveryIndex = InferDeliveryIndex(text, merged);

        return (title.Trim(), description, deliveryIndex, merged, warnings);
    }

    public static IReadOnlyList<(string Name, string Color, decimal DefaultPct)> DefaultPartners() =>
    [
        ("Ortak 1", PartnerColors[0], 50m),
        ("Ortak 2", PartnerColors[1], 50m)
    ];

    public static List<(string Name, DateOnly Due, decimal Amount)> MergeSameDay(
        IReadOnlyList<(string Name, DateOnly Due, decimal Amount)> rows)
    {
        var result = new List<(string Name, DateOnly Due, decimal Amount)>();
        foreach (var group in rows.GroupBy(r => r.Due).OrderBy(g => g.Key))
        {
            var items = group.ToList();
            if (items.Count == 1)
            {
                result.Add(items[0]);
                continue;
            }

            var name = string.Join(" + ", items.Select(i => i.Name));
            if (name.Length > 280)
            {
                name = name[..277] + "…";
            }

            result.Add((name, group.Key, items.Sum(i => i.Amount)));
        }

        return result;
    }

    public static bool TryParseTrDate(string value, out DateOnly date)
    {
        value = value.Trim();
        return DateOnly.TryParseExact(
                   value,
                   ["dd.MM.yyyy", "d.M.yyyy", "dd/MM/yyyy", "yyyy-MM-dd"],
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.None,
                   out date)
               || DateOnly.TryParse(value, new CultureInfo("tr-TR"), DateTimeStyles.None, out date);
    }

    public static bool TryParseTrMoney(string value, out decimal amount)
    {
        amount = 0m;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var cleaned = value
            .Replace("₺", "", StringComparison.Ordinal)
            .Replace("TL", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" ", "", StringComparison.Ordinal)
            .Trim();

        // 1.070.000,00 or 1,070,000.00 or 250000.00
        if (cleaned.Contains(',') && cleaned.Contains('.'))
        {
            if (cleaned.LastIndexOf(',') > cleaned.LastIndexOf('.'))
            {
                // TR: 1.070.000,00
                cleaned = cleaned.Replace(".", "", StringComparison.Ordinal).Replace(',', '.');
            }
            else
            {
                // EN: 1,070,000.00
                cleaned = cleaned.Replace(",", "", StringComparison.Ordinal);
            }
        }
        else if (cleaned.Contains(',') && !cleaned.Contains('.'))
        {
            cleaned = cleaned.Replace(',', '.');
        }

        return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }

    private static string CleanupName(string name)
    {
        name = Regex.Replace(name, @"\s+", " ").Trim();
        name = name.Trim(':', '-', '·', '•', '|');
        return name;
    }

    private static bool IsNoiseName(string name)
    {
        var n = name.ToLowerInvariant();
        return n is "ay" or "taksit bilgileri" or "taksit tarihi" or "taksit tutarı"
               || n.Contains("sayfa", StringComparison.Ordinal)
               || n.Contains("geçtiğimiz ay", StringComparison.Ordinal)
               || n.Contains("sayın", StringComparison.Ordinal)
               || n.Contains("belge no", StringComparison.Ordinal)
               || n.Contains("kampanya", StringComparison.Ordinal)
               || n.Contains("taksit bilgileri", StringComparison.Ordinal);
    }

    private static string? InferTitle(string text)
    {
        var refMatch = CustomerRefRegex().Match(text);
        if (refMatch.Success)
        {
            var who = CleanupName(refMatch.Groups["who"].Value);
            return string.IsNullOrWhiteSpace(who) ? "Fuzul Ödeme Planı" : $"{who} — Fuzul Planı";
        }

        if (text.Contains("Fuzul", StringComparison.OrdinalIgnoreCase))
        {
            return "Fuzul Ev Ödeme Planı";
        }

        if (text.Contains("Eminevim", StringComparison.OrdinalIgnoreCase))
        {
            return "Eminevim Ödeme Planı";
        }

        return null;
    }

    private static string InferDescription(string text, decimal total)
    {
        var sb = new StringBuilder();
        if (text.Contains("Kampanyalı", StringComparison.OrdinalIgnoreCase))
        {
            sb.Append("Kampanyalı finansman planı");
        }
        else if (text.Contains("Konut", StringComparison.OrdinalIgnoreCase))
        {
            sb.Append("Konut ödeme planı");
        }
        else
        {
            sb.Append("Dosyadan aktarılan ödeme planı");
        }

        if (total > 0)
        {
            sb.Append(CultureInfo.GetCultureInfo("tr-TR"), $" (≈ {total:N2} ₺)");
        }

        var belgeno = BelgeNoRegex().Match(text);
        if (belgeno.Success)
        {
            sb.Append($" · Belge No: {belgeno.Groups[1].Value}");
        }

        return sb.ToString();
    }

    private static int InferDeliveryIndex(string text, IReadOnlyList<(string Name, DateOnly Due, decimal Amount)> rows)
    {
        if (rows.Count == 0)
        {
            return -1;
        }

        for (var i = 0; i < rows.Count; i++)
        {
            if (rows[i].Name.Contains("Tahsisat", StringComparison.OrdinalIgnoreCase)
                && !rows[i].Name.Contains("Dönemi", StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        var period = TahsisatDonemiRegex().Match(text);
        if (period.Success && int.TryParse(period.Groups[1].Value, out var month) && month >= 1)
        {
            // Prefer kampanyalı column's period when two appear; last match often right column
            var matches = TahsisatDonemiRegex().Matches(text);
            if (matches.Count > 0)
            {
                int.TryParse(matches[^1].Groups[1].Value, out month);
            }

            // After same-day merge, "month number" ≠ row index; find by name containing period
            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].Name.Contains($"{month}.", StringComparison.Ordinal)
                    || rows[i].Name.Contains($" {month} ", StringComparison.Ordinal))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    [GeneratedRegex(
        @"(?<![0-9])(?<ay>\d{1,2})\s+(?<name>(?:(?!\d{1,2}\.\d{1,2}\.\d{4}).){1,80}?)\s+(?<date>\d{1,2}\.\d{1,2}\.\d{4})\s+(?<amount>[\d.,]+)\s*₺",
        RegexOptions.CultureInvariant)]
    private static partial Regex InstallmentLineRegex();

    [GeneratedRegex(@"Belge\s*No\s*:\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BelgeNoRegex();

    [GeneratedRegex(@"Tahsisat\s*Dönemi\s*:\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TahsisatDonemiRegex();

    [GeneratedRegex(
        @"(?<who>[A-ZÇĞİÖŞÜa-zçğıöşü\s\.]+?)\s+REF\s*\(\d+\)",
        RegexOptions.CultureInvariant)]
    private static partial Regex CustomerRefRegex();
}
