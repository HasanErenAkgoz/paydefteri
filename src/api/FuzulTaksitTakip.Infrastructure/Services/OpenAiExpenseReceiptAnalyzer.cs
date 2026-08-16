using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FuzulTaksitTakip.Application.Common.Exceptions;
using FuzulTaksitTakip.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace FuzulTaksitTakip.Infrastructure.Services;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAI";
    public string ApiKey { get; set; } = string.Empty;
    public string ReceiptModel { get; set; } = "gpt-5.6-luna";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";
}

public interface IOpenAiExpenseReceiptAnalyzer : IExpenseReceiptAnalyzer
{
}

public sealed class OpenAiExpenseReceiptAnalyzer : IOpenAiExpenseReceiptAnalyzer
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;

    public OpenAiExpenseReceiptAnalyzer(HttpClient httpClient, IOptions<OpenAiOptions> options)
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
                "Fiş analizi henüz yapılandırılmamış. Sunucuda OpenAI API anahtarı eksik.");
        }

        var imageUrl = $"data:{input.ContentType};base64,{Convert.ToBase64String(input.Content)}";
        var prompt = ExpenseReceiptAnalysisProtocol.BuildPrompt(input.CategoryNames);

        using var request = new HttpRequestMessage(HttpMethod.Post, "responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = JsonContent.Create(new
        {
            model = _options.ReceiptModel,
            store = false,
            safety_identifier = input.SafetyIdentifier,
            reasoning = new { effort = "none" },
            max_output_tokens = 800,
            input = new object[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "input_text", text = prompt },
                        new { type = "input_image", image_url = imageUrl, detail = "high" },
                    },
                },
            },
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "expense_receipt",
                    strict = true,
                    schema = ExpenseReceiptAnalysisProtocol.CreateSchema(),
                },
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
            throw new ExternalServiceUnavailableException("Fiş analiz servisine ulaşılamadı.", exception);
        }

        using (response)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new ExternalServiceUnavailableException(
                    $"Fiş analiz servisi isteği tamamlayamadı ({(int)response.StatusCode}).");
            }

            try
            {
                return ParseResponse(responseBody);
            }
            catch (Exception exception) when (
                exception is JsonException or FormatException or InvalidOperationException or KeyNotFoundException)
            {
                throw new ExternalServiceUnavailableException("Fiş analiz sonucu okunamadı.", exception);
            }
        }
    }

    private static ExpenseReceiptAnalysisResult ParseResponse(string responseBody)
    {
        using var response = JsonDocument.Parse(responseBody);
        var outputText = response.RootElement.GetProperty("output")
            .EnumerateArray()
            .Where(item => item.TryGetProperty("type", out var type) && type.GetString() == "message")
            .SelectMany(item => item.GetProperty("content").EnumerateArray())
            .First(item => item.TryGetProperty("type", out var type) && type.GetString() == "output_text")
            .GetProperty("text")
            .GetString() ?? throw new JsonException("Output text is missing.");

        return ExpenseReceiptAnalysisProtocol.ParseResultJson(outputText);
    }
}
