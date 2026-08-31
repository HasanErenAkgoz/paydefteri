using System.Globalization;
using System.Text;

namespace PayDefteri.Domain.Templates;

public static class SpendingCategoryCatalog
{
    public static readonly IReadOnlyList<string> All =
    [
        "Market", "Restoran / Yemek", "Yemek Siparişi", "Akaryakıt",
        "Ulaşım / Taksi", "Araç", "Giyim", "Elektronik", "Eğlence", "Oyun",
        "Dijital Abonelikler", "Telefon / İnternet", "Elektrik / Su / Doğalgaz",
        "Sağlık", "Eğitim", "Seyahat", "Otel", "Online Alışveriş",
        "Ev / Mobilya", "Kişisel Bakım", "Sigorta", "Vergi", "Diğer",
    ];

    public static string Categorize(string description)
    {
        var value = Fold(description);
        if (ContainsAny(value, "market", "migros", "carrefour", "a101", "bim", "sok market")) return "Market";
        if (ContainsAny(value, "getir yemek", "yemeksepeti", "trendyol yemek")) return "Yemek Siparişi";
        if (ContainsAny(value, "restoran", "restaurant", "cafe", "kahve", "yemek")) return "Restoran / Yemek";
        if (ContainsAny(value, "benzin", "petrol", "opet", "shell")) return "Akaryakıt";
        if (ContainsAny(value, "taksi", "uber", "metro", "otobus")) return "Ulaşım / Taksi";
        if (ContainsAny(value, "elektrik", "dogalgaz", "su fatur")) return "Elektrik / Su / Doğalgaz";
        if (ContainsAny(value, "internet", "telefon", "turkcell", "vodafone")) return "Telefon / İnternet";
        if (ContainsAny(value, "eczane", "hastane", "saglik", "medical")) return "Sağlık";
        if (ContainsAny(value, "netflix", "spotify", "youtube premium", "apple music")) return "Dijital Abonelikler";
        if (ContainsAny(value, "oyun", "game", "steam", "playstation")) return "Oyun";
        if (ContainsAny(value, "sinema", "tiyatro", "konser")) return "Eğlence";
        if (ContainsAny(value, "okul", "kurs", "kitap", "egitim")) return "Eğitim";
        if (ContainsAny(value, "trendyol", "hepsiburada", "amazon")) return "Online Alışveriş";
        if (ContainsAny(value, "giyim", "moda", "zara", "lc waikiki")) return "Giyim";
        if (ContainsAny(value, "elektronik", "teknosa", "mediamarkt")) return "Elektronik";
        return "Diğer";
    }

    private static bool ContainsAny(string value, params string[] terms) => terms.Any(value.Contains);

    private static string Fold(string value)
    {
        var normalized = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var result = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                result.Append(character == 'ı' ? 'i' : character);
            }
        }
        return result.ToString().Normalize(NormalizationForm.FormC);
    }
}
