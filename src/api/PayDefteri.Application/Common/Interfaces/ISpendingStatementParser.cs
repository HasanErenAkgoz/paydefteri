namespace PayDefteri.Application.Common.Interfaces;

public sealed record ParsedSpendingTransaction(
    DateOnly OccurredOn,
    string Description,
    string MerchantName,
    decimal Amount,
    bool IsRefund,
    string Currency,
    string Category,
    int? InstallmentCurrent,
    int? InstallmentTotal);

public sealed record ParsedSpendingStatement(
    string SourceKind,
    IReadOnlyList<ParsedSpendingTransaction> Transactions,
    IReadOnlyList<string> Warnings);

public interface ISpendingStatementParser
{
    Task<ParsedSpendingStatement> ParseAsync(
        byte[] content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);
}
