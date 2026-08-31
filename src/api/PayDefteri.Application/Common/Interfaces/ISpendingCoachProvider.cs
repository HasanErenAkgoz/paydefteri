namespace PayDefteri.Application.Common.Interfaces;

public sealed record SpendingCoachProviderRequest(
    string Purpose,
    IReadOnlyDictionary<string, decimal> Evidence,
    IReadOnlyDictionary<string, string> Facts,
    string? Question = null);

public sealed record SpendingCoachProviderResult(
    string Answer,
    IReadOnlyList<string> Insights,
    IReadOnlyList<string> Recommendations,
    IReadOnlyList<string> EvidenceKeys);

public interface ISpendingCoachProvider
{
    Task<SpendingCoachProviderResult> GenerateAsync(
        SpendingCoachProviderRequest request,
        CancellationToken cancellationToken = default);
}
