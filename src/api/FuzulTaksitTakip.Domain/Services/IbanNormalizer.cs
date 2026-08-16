using System.Text.RegularExpressions;

namespace FuzulTaksitTakip.Domain.Services;

public static partial class IbanNormalizer
{
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var cleaned = value.Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();

        return cleaned;
    }

    public static bool IsValidTurkishIban(string? value)
    {
        var normalized = Normalize(value);
        if (normalized is null)
        {
            return false;
        }

        return TurkishIbanRegex().IsMatch(normalized);
    }

    [GeneratedRegex(@"^TR\d{24}$")]
    private static partial Regex TurkishIbanRegex();
}
