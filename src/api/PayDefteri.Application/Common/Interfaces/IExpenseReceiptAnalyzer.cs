namespace PayDefteri.Application.Common.Interfaces;

public sealed record ExpenseReceiptAnalysisInput(
    string ContentType,
    byte[] Content,
    IReadOnlyList<string> CategoryNames,
    string SafetyIdentifier);

public sealed record ExpenseReceiptAnalysisResult(
    string? MerchantName,
    decimal? TotalAmount,
    DateOnly? OccurredOn,
    string? CategoryName,
    int? InstallmentCount,
    string? DocumentNumber,
    string? Note,
    decimal Confidence,
    IReadOnlyList<string> LowConfidenceFields,
    IReadOnlyList<string> Warnings);

public interface IExpenseReceiptAnalyzer
{
    Task<ExpenseReceiptAnalysisResult> AnalyzeAsync(
        ExpenseReceiptAnalysisInput input,
        CancellationToken cancellationToken = default);
}
