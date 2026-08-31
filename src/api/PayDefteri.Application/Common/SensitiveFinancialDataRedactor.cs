using System.Text.RegularExpressions;

namespace PayDefteri.Application.Common;

public static partial class SensitiveFinancialDataRedactor
{
    public static string Redact(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var redacted = CvvPattern().Replace(value, "$1 [GİZLİ]");
        redacted = PanPattern().Replace(redacted, "[KART REDAKTE]");
        return WhitespacePattern().Replace(redacted, " ").Trim();
    }

    [GeneratedRegex(@"\b(CVV|CVC|GÜVENLİK\s*KODU)\s*[:#-]?\s*\d{3,4}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CvvPattern();

    [GeneratedRegex(@"(?<!\d)(?:\d[ -]?){13,19}(?!\d)", RegexOptions.CultureInvariant)]
    private static partial Regex PanPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
