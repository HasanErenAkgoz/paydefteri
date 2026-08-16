using System.Net.Http.Json;
using System.Text.Json;
using PayDefteri.Application.Common.Exceptions;
using PayDefteri.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace PayDefteri.Infrastructure.Services;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";
    public string ApiKey { get; set; } = string.Empty;
    public string ReceiptModel { get; set; } = "gemini-3.1-flash-lite";
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/";
}

public interface IGeminiExpenseReceiptAnalyzer : IExpenseReceiptAnalyzer
{
}

public sealed class GeminiExpenseReceiptAnalyzer : IGeminiExpenseReceiptAnalyzer
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;

    public GeminiExpenseReceiptAnalyzer(HttpClient httpClient, IOptions<GeminiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<ExpenseReceiptAnalysisResult> AnalyzeAsync(
        ExpenseReceiptAnalysisInput input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new ExternalServiceUnavailableException(
                "Fiş analizi henüz yapılandırılmamış. Sunucuda Gemini API anahtarı eksik.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "interactions");
        request.Headers.Add("x-goog-api-key", _options.ApiKey);
        request.Headers.Add("Api-Revision", "2026-05-20");
        request.Content = JsonContent.Create(new
        {
            model = _options.ReceiptModel,
            store = false,
            input = new object[]
            {
                new { type = "text", text = ExpenseReceiptAnalysisProtocol.BuildPrompt(input.CategoryNames) },
                new
                {
                    type = "image",
                    data = Convert.ToBase64String(input.Content),
                    mime_type = input.ContentType,
                    resolution = "high",
                },
            },
            response_format = new
            {
                type = "text",
                mime_type = "application/json",
                schema = ExpenseReceiptAnalysisProtocol.CreateSchema(),
            },
        });

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception exception) when (
            exception is HttpRequestException
            || exception is TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            throw new ExternalServiceUnavailableException("Gemini fiş analiz servisine ulaşılamadı.", exception);
        }

        using (response)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new ExternalServiceUnavailableException(
                    $"Gemini fiş analiz isteği tamamlanamadı ({(int)response.StatusCode}).");
            }

            try
            {
                return ParseResponse(responseBody);
            }
            catch (Exception exception) when (
                exception is JsonException or FormatException or InvalidOperationException or KeyNotFoundException)
            {
                throw new ExternalServiceUnavailableException("Gemini fiş analiz sonucu okunamadı.", exception);
            }
        }
    }

    private static ExpenseReceiptAnalysisResult ParseResponse(string responseBody)
    {
        using var response = JsonDocument.Parse(responseBody);
        var outputText = response.RootElement.GetProperty("steps")
            .EnumerateArray()
            .Where(step => step.TryGetProperty("type", out var type) && type.GetString() == "model_output")
            .SelectMany(step => step.GetProperty("content").EnumerateArray())
            .First(content => content.TryGetProperty("type", out var type) && type.GetString() == "text")
            .GetProperty("text")
            .GetString() ?? throw new JsonException("Gemini output text is missing.");

        return ExpenseReceiptAnalysisProtocol.ParseResultJson(outputText);
    }
}
