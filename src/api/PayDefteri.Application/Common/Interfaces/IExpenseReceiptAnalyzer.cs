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

public sealed record CreditCardStatementAnalysisInput(
    string ContentType,
    byte[] Content,
    string FileName,
    IReadOnlyList<string> CategoryNames,
    string SafetyIdentifier);

public sealed record CreditCardStatementTransaction(
    DateOnly OccurredOn,
    string Description,
    decimal Amount,
    bool IsIncomeOrRefund,
    int? InstallmentCurrent,
    int? InstallmentTotal,
    string? CategoryName,
    string? MerchantName,
    decimal Confidence,
    string? Note);

public sealed record CreditCardStatementAnalysis(
    string? BankName,
    string? CardLastDigits,
    string? StatementPeriod,
    DateOnly? DueDate,
    decimal? TotalDebt,
    decimal? MinimumPayment,
    IReadOnlyList<CreditCardStatementTransaction> Transactions,
    IReadOnlyList<string> Warnings);

public interface ICreditCardStatementAnalyzer
{
    Task<CreditCardStatementAnalysis> AnalyzeAsync(
        CreditCardStatementAnalysisInput input,
        CancellationToken cancellationToken = default);
}
