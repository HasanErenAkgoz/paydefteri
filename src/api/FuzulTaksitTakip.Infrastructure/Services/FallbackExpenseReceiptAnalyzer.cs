using FuzulTaksitTakip.Application.Common.Exceptions;
using FuzulTaksitTakip.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace FuzulTaksitTakip.Infrastructure.Services;

public sealed class FallbackExpenseReceiptAnalyzer : IExpenseReceiptAnalyzer
{
    private readonly IGeminiExpenseReceiptAnalyzer _primary;
    private readonly IOpenAiExpenseReceiptAnalyzer _fallback;
    private readonly ILogger<FallbackExpenseReceiptAnalyzer> _logger;

    public FallbackExpenseReceiptAnalyzer(
        IGeminiExpenseReceiptAnalyzer primary,
        IOpenAiExpenseReceiptAnalyzer fallback,
        ILogger<FallbackExpenseReceiptAnalyzer> logger)
    {
        _primary = primary;
        _fallback = fallback;
        _logger = logger;
    }

    public async Task<ExpenseReceiptAnalysisResult> AnalyzeAsync(
        ExpenseReceiptAnalysisInput input,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _primary.AnalyzeAsync(input, cancellationToken);
        }
        catch (ExternalServiceUnavailableException primaryException)
        {
            _logger.LogWarning(primaryException, "Gemini receipt analysis failed; trying OpenAI fallback.");
        }

        try
        {
            return await _fallback.AnalyzeAsync(input, cancellationToken);
        }
        catch (ExternalServiceUnavailableException fallbackException)
        {
            throw new ExternalServiceUnavailableException(
                "Fiş analiz servisine ulaşılamadı.",
                fallbackException);
        }
    }
}
