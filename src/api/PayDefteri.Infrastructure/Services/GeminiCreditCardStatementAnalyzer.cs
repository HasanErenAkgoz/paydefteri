using PayDefteri.Application.Common.Interfaces;

namespace PayDefteri.Infrastructure.Services;

/// <summary>
/// Adapts the shared statement parser for the legacy plan expense-import flow.
/// </summary>
public sealed class GeminiCreditCardStatementAnalyzer : ICreditCardStatementAnalyzer
{
    private readonly ISpendingStatementParser _parser;

    public GeminiCreditCardStatementAnalyzer(ISpendingStatementParser parser)
    {
        _parser = parser;
    }

    public async Task<CreditCardStatementAnalysis> AnalyzeAsync(
        CreditCardStatementAnalysisInput input,
        CancellationToken cancellationToken = default)
    {
        var statement = await _parser.ParseAsync(
            input.Content,
            input.FileName,
            input.ContentType,
            cancellationToken);

        var transactions = statement.Transactions.Select(transaction =>
        {
            var category = input.CategoryNames.FirstOrDefault(name =>
                string.Equals(name, transaction.Category, StringComparison.OrdinalIgnoreCase));
            return new CreditCardStatementTransaction(
                transaction.OccurredOn,
                transaction.Description,
                transaction.Amount,
                transaction.IsRefund,
                transaction.InstallmentCurrent,
                transaction.InstallmentTotal,
                category,
                transaction.MerchantName,
                0.9m,
                $"{statement.SourceKind} ekstresinden aktarıldı.");
        }).ToList();

        return new CreditCardStatementAnalysis(
            null,
            null,
            null,
            null,
            null,
            null,
            transactions,
            statement.Warnings);
    }
}
