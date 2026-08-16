using System.Globalization;
using System.Text.Json;
using PayDefteri.Application.Common.Interfaces;

namespace PayDefteri.Infrastructure.Services;

internal static class ExpenseReceiptAnalysisProtocol
{
    public static string BuildPrompt(IReadOnlyList<string> categoryNames)
    {
        var categoryList = categoryNames.Count == 0
            ? "Kategori tanımlı değil; categoryName null olmalı."
            : $"categoryName yalnızca şu değerlerden biri veya null olmalı: {string.Join(", ", categoryNames)}";

        return $"""
            Bu görseldeki Türkçe veya yabancı fiş, fatura ya da kredi kartı slipini analiz et.
            Vergi/KDV ara toplamını değil, ödenen genel toplamı çıkar. Tarihi YYYY-MM-DD biçiminde ver.
            Taksit sayısını yalnızca belgede açıkça görünüyorsa yaz; tahmin etme.
            Belge okunamıyorsa ilgili alanı null bırak ve lowConfidenceFields ile warnings alanlarına ekle.
            {categoryList}
            Kullanıcı onaylamadan hiçbir kayıt oluşturulmayacak.
            """;
    }

    public static object CreateSchema() => new
    {
        type = "object",
        additionalProperties = false,
        properties = new
        {
            merchantName = new { type = new[] { "string", "null" } },
            totalAmount = new { type = new[] { "number", "null" } },
            occurredOn = new { type = new[] { "string", "null" } },
            categoryName = new { type = new[] { "string", "null" } },
            installmentCount = new { type = new[] { "integer", "null" } },
            documentNumber = new { type = new[] { "string", "null" } },
            note = new { type = new[] { "string", "null" } },
            confidence = new { type = "number", minimum = 0, maximum = 1 },
            lowConfidenceFields = new
            {
                type = "array",
                items = new
                {
                    type = "string",
                    @enum = new[] { "name", "totalAmount", "occurredOn", "categoryName", "installmentCount" },
                },
            },
            warnings = new { type = "array", items = new { type = "string" } },
        },
        required = new[]
        {
            "merchantName", "totalAmount", "occurredOn", "categoryName", "installmentCount",
            "documentNumber", "note", "confidence", "lowConfidenceFields", "warnings",
        },
    };

    public static ExpenseReceiptAnalysisResult ParseResultJson(string resultJson)
    {
        using var result = JsonDocument.Parse(resultJson);
        var root = result.RootElement;

        return new ExpenseReceiptAnalysisResult(
            NullableString(root, "merchantName"),
            NullableDecimal(root, "totalAmount"),
            ParseDate(NullableString(root, "occurredOn")),
            NullableString(root, "categoryName"),
            NullableInt(root, "installmentCount"),
            NullableString(root, "documentNumber"),
            NullableString(root, "note"),
            root.GetProperty("confidence").GetDecimal(),
            StringArray(root, "lowConfidenceFields"),
            StringArray(root, "warnings"));
    }

    private static string? NullableString(JsonElement root, string name)
        => root.GetProperty(name).ValueKind == JsonValueKind.Null ? null : root.GetProperty(name).GetString();

    private static decimal? NullableDecimal(JsonElement root, string name)
        => root.GetProperty(name).ValueKind == JsonValueKind.Null ? null : root.GetProperty(name).GetDecimal();

    private static int? NullableInt(JsonElement root, string name)
        => root.GetProperty(name).ValueKind == JsonValueKind.Null ? null : root.GetProperty(name).GetInt32();

    private static DateOnly? ParseDate(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string[] StringArray(JsonElement root, string name)
        => root.GetProperty(name).EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();
}
