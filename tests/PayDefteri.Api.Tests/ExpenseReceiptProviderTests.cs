using System.Net;
using System.Text;
using System.Text.Json;
using PayDefteri.Application.Common.Exceptions;
using PayDefteri.Application.Common.Interfaces;
using PayDefteri.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace PayDefteri.Api.Tests;

public sealed class ExpenseReceiptProviderTests
{
    [Fact]
    public async Task Gemini_request_contains_image_schema_and_api_key_and_maps_response()
    {
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse(CreateGeminiResponse(0.92m)));
        var analyzer = CreateGeminiAnalyzer(handler);

        var result = await analyzer.AnalyzeAsync(CreateInput());

        result.MerchantName.Should().Be("Örnek Market");
        result.TotalAmount.Should().Be(123.45m);
        result.OccurredOn.Should().Be(new DateOnly(2026, 8, 7));
        result.CategoryName.Should().Be("Market");
        result.InstallmentCount.Should().Be(3);
        result.Confidence.Should().Be(0.92m);

        handler.RequestUri.Should().Be("https://generativelanguage.googleapis.com/v1beta/interactions");
        handler.ApiKey.Should().Be("gemini-test-key");
        handler.ApiRevision.Should().Be("2026-05-20");

        using var body = JsonDocument.Parse(handler.RequestBody!);
        var root = body.RootElement;
        root.GetProperty("model").GetString().Should().Be("gemini-3.1-flash-lite");
        root.GetProperty("store").GetBoolean().Should().BeFalse();

        var image = root.GetProperty("input")[1];
        image.GetProperty("type").GetString().Should().Be("image");
        image.GetProperty("data").GetString().Should().Be(Convert.ToBase64String([1, 2, 3]));
        image.GetProperty("mime_type").GetString().Should().Be("image/jpeg");
        image.GetProperty("resolution").GetString().Should().Be("high");

        var responseFormat = root.GetProperty("response_format");
        responseFormat.GetProperty("mime_type").GetString().Should().Be("application/json");
        responseFormat.GetProperty("schema").GetProperty("required")
            .EnumerateArray().Select(item => item.GetString())
            .Should().Contain("totalAmount");
    }

    [Fact]
    public async Task Missing_gemini_key_uses_openai_fallback()
    {
        var primary = new GeminiExpenseReceiptAnalyzer(
            new HttpClient(new RecordingHttpMessageHandler(_ => throw new InvalidOperationException())),
            Options.Create(new GeminiOptions()));
        var fallback = new StubOpenAiAnalyzer(Result(0.85m));
        var analyzer = CreateFallback(primary, fallback);

        var result = await analyzer.AnalyzeAsync(CreateInput());

        result.Confidence.Should().Be(0.85m);
        fallback.CallCount.Should().Be(1);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Gemini_http_failure_uses_openai_fallback(HttpStatusCode statusCode)
    {
        var primary = CreateGeminiAnalyzer(new RecordingHttpMessageHandler(
            _ => new HttpResponseMessage(statusCode)));
        var fallback = new StubOpenAiAnalyzer(Result(0.81m));
        var analyzer = CreateFallback(primary, fallback);

        var result = await analyzer.AnalyzeAsync(CreateInput());

        result.Confidence.Should().Be(0.81m);
        fallback.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Gemini_timeout_uses_openai_fallback()
    {
        var primary = CreateGeminiAnalyzer(new RecordingHttpMessageHandler(
            _ => throw new TaskCanceledException("timeout")));
        var fallback = new StubOpenAiAnalyzer(Result(0.79m));
        var analyzer = CreateFallback(primary, fallback);

        var result = await analyzer.AnalyzeAsync(CreateInput());

        result.Confidence.Should().Be(0.79m);
        fallback.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Malformed_gemini_response_uses_openai_fallback()
    {
        var primary = CreateGeminiAnalyzer(new RecordingHttpMessageHandler(
            _ => JsonResponse("{\"steps\":[]}")));
        var fallback = new StubOpenAiAnalyzer(Result(0.77m));
        var analyzer = CreateFallback(primary, fallback);

        var result = await analyzer.AnalyzeAsync(CreateInput());

        result.Confidence.Should().Be(0.77m);
        fallback.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Valid_low_confidence_gemini_result_does_not_use_fallback()
    {
        var primary = new StubGeminiAnalyzer(Result(0.2m));
        var fallback = new StubOpenAiAnalyzer(Result(0.99m));
        var analyzer = CreateFallback(primary, fallback);

        var result = await analyzer.AnalyzeAsync(CreateInput());

        result.Confidence.Should().Be(0.2m);
        primary.CallCount.Should().Be(1);
        fallback.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Both_provider_failures_return_safe_generic_error()
    {
        var primary = new StubGeminiAnalyzer(new ExternalServiceUnavailableException("primary detail"));
        var fallback = new StubOpenAiAnalyzer(new ExternalServiceUnavailableException("fallback detail"));
        var analyzer = CreateFallback(primary, fallback);

        var action = () => analyzer.AnalyzeAsync(CreateInput());

        var exception = await action.Should().ThrowAsync<ExternalServiceUnavailableException>();
        exception.Which.Message.Should().Be("Fiş analiz servisine ulaşılamadı.");
        exception.Which.Message.Should().NotContain("primary detail").And.NotContain("fallback detail");
    }

    private static GeminiExpenseReceiptAnalyzer CreateGeminiAnalyzer(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/"),
        };

        return new GeminiExpenseReceiptAnalyzer(client, Options.Create(new GeminiOptions
        {
            ApiKey = "gemini-test-key",
        }));
    }

    private static FallbackExpenseReceiptAnalyzer CreateFallback(
        IGeminiExpenseReceiptAnalyzer primary,
        IOpenAiExpenseReceiptAnalyzer fallback)
        => new(primary, fallback, NullLogger<FallbackExpenseReceiptAnalyzer>.Instance);

    private static ExpenseReceiptAnalysisInput CreateInput()
        => new("image/jpeg", [1, 2, 3], ["Market", "Ulaşım"], "user-hash");

    private static ExpenseReceiptAnalysisResult Result(decimal confidence)
        => new("Örnek Market", 123.45m, new DateOnly(2026, 8, 7), "Market", 3,
            "ABC-123", "Test", confidence, [], []);

    private static string CreateGeminiResponse(decimal confidence)
    {
        var result = JsonSerializer.Serialize(new
        {
            merchantName = "Örnek Market",
            totalAmount = 123.45m,
            occurredOn = "2026-08-07",
            categoryName = "Market",
            installmentCount = 3,
            documentNumber = "ABC-123",
            note = "Test",
            confidence,
            lowConfidenceFields = Array.Empty<string>(),
            warnings = Array.Empty<string>(),
        });

        return JsonSerializer.Serialize(new
        {
            status = "completed",
            steps = new[]
            {
                new
                {
                    type = "model_output",
                    content = new[] { new { type = "text", text = result } },
                },
            },
        });
    }

    private static HttpResponseMessage JsonResponse(string content)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json"),
        };

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public RecordingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public string? RequestUri { get; private set; }
        public string? RequestBody { get; private set; }
        public string? ApiKey { get; private set; }
        public string? ApiRevision { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri?.ToString();
            RequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            ApiKey = request.Headers.GetValues("x-goog-api-key").Single();
            ApiRevision = request.Headers.GetValues("Api-Revision").Single();
            return _responseFactory(request);
        }
    }

    private sealed class StubGeminiAnalyzer : IGeminiExpenseReceiptAnalyzer
    {
        private readonly ExpenseReceiptAnalysisResult? _result;
        private readonly Exception? _exception;

        public StubGeminiAnalyzer(ExpenseReceiptAnalysisResult result) => _result = result;
        public StubGeminiAnalyzer(Exception exception) => _exception = exception;
        public int CallCount { get; private set; }

        public Task<ExpenseReceiptAnalysisResult> AnalyzeAsync(
            ExpenseReceiptAnalysisInput input,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return _exception is null
                ? Task.FromResult(_result!)
                : Task.FromException<ExpenseReceiptAnalysisResult>(_exception);
        }
    }

    private sealed class StubOpenAiAnalyzer : IOpenAiExpenseReceiptAnalyzer
    {
        private readonly ExpenseReceiptAnalysisResult? _result;
        private readonly Exception? _exception;

        public StubOpenAiAnalyzer(ExpenseReceiptAnalysisResult result) => _result = result;
        public StubOpenAiAnalyzer(Exception exception) => _exception = exception;
        public int CallCount { get; private set; }

        public Task<ExpenseReceiptAnalysisResult> AnalyzeAsync(
            ExpenseReceiptAnalysisInput input,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return _exception is null
                ? Task.FromResult(_result!)
                : Task.FromException<ExpenseReceiptAnalysisResult>(_exception);
        }
    }
}
