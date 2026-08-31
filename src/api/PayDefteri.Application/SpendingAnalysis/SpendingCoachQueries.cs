using System.Globalization;
using System.Text.RegularExpressions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PayDefteri.Application.Common.Exceptions;
using PayDefteri.Application.Common.Interfaces;

namespace PayDefteri.Application.SpendingAnalysis;

public sealed record SpendingCoachResponseDto(
    string Answer,
    IReadOnlyList<string> Insights,
    IReadOnlyList<string> Recommendations,
    IReadOnlyDictionary<string, decimal> Evidence,
    IReadOnlyList<string> EvidenceKeys,
    string Disclaimer,
    bool DataAvailable,
    bool AiGenerated);

public sealed record GetSpendingCoachQuery(Guid StatementId) : IRequest<SpendingCoachResponseDto>;
public sealed record AskSpendingCoachQuery(Guid StatementId, string Question) : IRequest<SpendingCoachResponseDto>;

public sealed class AskSpendingCoachQueryValidator : AbstractValidator<AskSpendingCoachQuery>
{
    public AskSpendingCoachQueryValidator()
    {
        RuleFor(x => x.Question).NotEmpty().MaximumLength(500);
    }
}

public sealed class SpendingCoachQueryHandler :
    IRequestHandler<GetSpendingCoachQuery, SpendingCoachResponseDto>,
    IRequestHandler<AskSpendingCoachQuery, SpendingCoachResponseDto>
{
    private const string Disclaimer =
        "Bu içerik yalnızca yüklediğiniz ekstre verilerine dayalı genel bilgilendirmedir; finansal tavsiye değildir.";
    private static readonly Regex LargestCountRegex = new(@"en\s+büyük\s+(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex NumberRegex = new(
        @"(?<![\p{L}\d])(?:\d{1,3}(?:\.\d{3})+,\d+|\d{1,3}(?:,\d{3})+\.\d+|\d+(?:[.,]\d+)?)(?![\p{L}\d])",
        RegexOptions.CultureInvariant);
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _user;
    private readonly ISender _sender;
    private readonly ISpendingCoachProvider _provider;

    public SpendingCoachQueryHandler(
        IAppDbContext db,
        ICurrentUser user,
        ISender sender,
        ISpendingCoachProvider provider)
    {
        _db = db;
        _user = user;
        _sender = sender;
        _provider = provider;
    }

    public async Task<SpendingCoachResponseDto> Handle(GetSpendingCoachQuery request, CancellationToken ct)
    {
        var context = await LoadContext(request.StatementId, ct);
        if (context.Transactions.Count == 0)
        {
            return NoData("Bu ekstrede analiz edilebilecek harcama bulunmuyor.");
        }

        var evidence = BuildCoreEvidence(context.Dashboard);
        var fallback = BuildCoachFallback(context.Dashboard, evidence);
        return await TryProvider(
            new SpendingCoachProviderRequest(
                "Ekstreyi kısa ve tarafsız biçimde özetle. Yalnızca verilen kanıtları kullan.",
                evidence,
                BuildFacts(context)),
            fallback,
            ct);
    }

    public async Task<SpendingCoachResponseDto> Handle(AskSpendingCoachQuery request, CancellationToken ct)
    {
        var context = await LoadContext(request.StatementId, ct);
        if (context.Transactions.Count == 0)
        {
            return NoData("Bu soruyu yanıtlamak için ekstrede harcama verisi bulunmuyor.");
        }

        var deterministic = TryAnswerCommonIntent(request.Question, context);
        if (deterministic is not null)
        {
            return deterministic;
        }

        var evidence = BuildCoreEvidence(context.Dashboard);
        var fallback = Response(
            "Bu soruyu mevcut ekstre verileriyle güvenilir biçimde yanıtlayamadım. En büyük işlemler, mağaza veya kategori toplamı, hafta sonu harcaması, önceki ay karşılaştırması ya da düzenli ödemeleri sorabilirsiniz.",
            [],
            [],
            evidence,
            evidence.Keys.Take(1).ToList(),
            false);
        return await TryProvider(
            new SpendingCoachProviderRequest(
                "Kullanıcının sorusunu yalnızca verilen kanıt ve olgularla yanıtla.",
                evidence,
                BuildFacts(context),
                request.Question),
            fallback,
            ct);
    }

    private async Task<CoachContext> LoadContext(Guid statementId, CancellationToken ct)
    {
        var owner = UploadSpendingStatementCommandHandler.RequireUserId(_user);
        var exists = await _db.SpendingStatements.AsNoTracking().AnyAsync(
            x => x.Id == statementId && x.OwnerUserId == owner, ct);
        if (!exists)
        {
            throw new NotFoundException("Ekstre bulunamadı.");
        }
        var transactions = await _db.SpendingStatements.AsNoTracking()
            .Where(x => x.Id == statementId && x.OwnerUserId == owner)
            .SelectMany(x => x.Transactions)
            .Where(x => !x.IsRefund)
            .OrderByDescending(x => x.Amount)
            .Select(x => new CoachTransaction(x.OccurredOn, x.MerchantName, x.Amount, x.Currency, x.Category))
            .ToListAsync(ct);

        var dashboard = await _sender.Send(new GetSpendingDashboardQuery(statementId), ct);
        var patterns = await _sender.Send(new GetSpendingPatternsQuery(statementId), ct);
        return new(transactions, dashboard, patterns);
    }

    private async Task<SpendingCoachResponseDto> TryProvider(
        SpendingCoachProviderRequest request,
        SpendingCoachResponseDto fallback,
        CancellationToken ct)
    {
        try
        {
            var generated = await _provider.GenerateAsync(request, ct);
            if (!IsGrounded(generated, request.Evidence)) return fallback;
            var cited = generated.EvidenceKeys.Distinct().ToList();
            return Response(generated.Answer, generated.Insights, generated.Recommendations,
                request.Evidence, cited, true);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            return fallback;
        }
    }

    private static bool IsGrounded(
        SpendingCoachProviderResult result,
        IReadOnlyDictionary<string, decimal> evidence)
    {
        if (string.IsNullOrWhiteSpace(result.Answer)
            || result.EvidenceKeys.Count == 0
            || result.EvidenceKeys.Any(key => !evidence.ContainsKey(key)))
        {
            return false;
        }

        var allowed = result.EvidenceKeys.Select(key => evidence[key]).ToList();
        var text = string.Join(' ', result.Answer, string.Join(' ', result.Insights), string.Join(' ', result.Recommendations));
        return NumberRegex.Matches(text).Select(match => ParseNumber(match.Value))
            .All(number => number is not null && allowed.Any(value => value == number.Value));
    }

    private static decimal? ParseNumber(string value)
    {
        var normalized = value;
        if (value.Contains(',') && value.Contains('.'))
        {
            normalized = value.LastIndexOf(',') > value.LastIndexOf('.')
                ? value.Replace(".", "").Replace(',', '.')
                : value.Replace(",", "");
        }
        else if (value.Contains(','))
        {
            normalized = value.Replace(',', '.');
        }
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
            ? number
            : null;
    }

    private static SpendingCoachResponseDto? TryAnswerCommonIntent(string question, CoachContext context)
    {
        var normalized = question.ToLower(new CultureInfo("tr-TR"));
        if (normalized.Contains("en büyük", StringComparison.Ordinal)) return Largest(normalized, context.Transactions);
        if (normalized.Contains("hafta son", StringComparison.Ordinal)) return Weekend(context.Transactions);
        if (normalized.Contains("karşılaştır", StringComparison.Ordinal)
            || normalized.Contains("önceki ay", StringComparison.Ordinal)
            || normalized.Contains("geçen ay", StringComparison.Ordinal)) return Comparison(context.Patterns);
        if (normalized.Contains("düzenli", StringComparison.Ordinal)
            || normalized.Contains("tekrarla", StringComparison.Ordinal)
            || normalized.Contains("abonelik", StringComparison.Ordinal)) return Recurring(context.Patterns);
        if (normalized.Contains("toplam", StringComparison.Ordinal)) return NamedTotal(normalized, context.Transactions);
        return null;
    }

    private static SpendingCoachResponseDto Largest(string question, IReadOnlyList<CoachTransaction> transactions)
    {
        var match = LargestCountRegex.Match(question);
        var count = match.Success && int.TryParse(match.Groups[1].Value, out var requested)
            ? Math.Clamp(requested, 1, 10)
            : 5;
        var evidence = new Dictionary<string, decimal>();
        var lines = new List<string>();
        foreach (var currencyGroup in transactions.GroupBy(x => x.Currency).OrderBy(x => x.Key))
        {
            var items = currencyGroup.OrderByDescending(x => x.Amount).Take(count).ToList();
            for (var index = 0; index < items.Count; index++)
            {
                var key = $"largest_{index + 1}_amount_{currencyGroup.Key}";
                evidence[key] = items[index].Amount;
                lines.Add($"{items[index].MerchantName}: {Money(items[index].Amount)} {currencyGroup.Key}");
            }
        }
        return Response(string.Join("; ", lines), [], [], evidence, evidence.Keys.ToList(), false);
    }

    private static SpendingCoachResponseDto Weekend(IReadOnlyList<CoachTransaction> transactions)
    {
        var evidence = transactions.Where(x => x.OccurredOn.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            .GroupBy(x => x.Currency).ToDictionary(x => $"weekend_total_{x.Key}", x => x.Sum(y => y.Amount));
        var answer = evidence.Count == 0
            ? "Bu ekstrede hafta sonuna ait harcama bulunmuyor."
            : "Hafta sonu toplamı: " + string.Join(", ", evidence.Select(x => $"{Money(x.Value)} {CurrencyFromKey(x.Key)}"));
        return Response(answer, [], [], evidence, evidence.Keys.ToList(), false);
    }

    private static SpendingCoachResponseDto Comparison(SpendingPatternsDto patterns)
    {
        var rows = patterns.PreviousStatementComparison?.Currencies ?? [];
        if (rows.Count == 0) return NoData("Karşılaştırma için önceki bir ekstre bulunmuyor.");
        var evidence = rows.ToDictionary(x => $"comparison_change_{x.Currency}", x => x.ChangeAmount);
        var answer = string.Join("; ", rows.Select(x =>
            $"{x.Currency}: önceki ekstreye göre {Money(Math.Abs(x.ChangeAmount))} {(x.ChangeAmount >= 0 ? "artış" : "azalış")}"));
        return Response(answer, [], [], evidence, evidence.Keys.ToList(), false);
    }

    private static SpendingCoachResponseDto Recurring(SpendingPatternsDto patterns)
    {
        if (patterns.RecurringCandidates.Count == 0) return NoData("Düzenli ödeme adayı bulunamadı; bunun için en az iki dönem gerekir.");
        var evidence = new Dictionary<string, decimal>();
        var lines = patterns.RecurringCandidates.Select((x, index) =>
        {
            evidence[$"recurring_{index + 1}_total_{x.Currency}"] = x.TotalAmount;
            return $"{x.MerchantName}: {Money(x.TotalAmount)} {x.Currency}";
        }).ToList();
        return Response(string.Join("; ", lines), [], [], evidence, evidence.Keys.ToList(), false);
    }

    private static SpendingCoachResponseDto? NamedTotal(string question, IReadOnlyList<CoachTransaction> transactions)
    {
        var named = transactions.Where(x =>
            question.Contains(x.Category.ToLower(new CultureInfo("tr-TR")), StringComparison.Ordinal)
            || question.Contains(x.MerchantName.ToLower(new CultureInfo("tr-TR")), StringComparison.Ordinal)).ToList();
        if (named.Count == 0) return null;
        var evidence = named.GroupBy(x => x.Currency).ToDictionary(x => $"named_total_{x.Key}", x => x.Sum(y => y.Amount));
        var answer = "İstenen toplam: " + string.Join(", ", evidence.Select(x => $"{Money(x.Value)} {CurrencyFromKey(x.Key)}"));
        return Response(answer, [], [], evidence, evidence.Keys.ToList(), false);
    }

    private static Dictionary<string, decimal> BuildCoreEvidence(SpendingDashboardDto dashboard)
    {
        var evidence = new Dictionary<string, decimal>();
        foreach (var currency in dashboard.Currencies)
        {
            evidence[$"total_spending_{currency.Currency}"] = currency.Spending;
            evidence[$"daily_average_{currency.Currency}"] = currency.DailyAverage;
            evidence[$"necessity_ratio_{currency.Currency}"] = currency.NecessityRatio;
            if (currency.Categories.FirstOrDefault() is { } top)
                evidence[$"top_category_amount_{currency.Currency}"] = top.Amount;
        }
        return evidence;
    }

    private static IReadOnlyDictionary<string, string> BuildFacts(CoachContext context)
    {
        var facts = new Dictionary<string, string>
        {
            ["period"] = string.Join(" - ", context.Transactions.Min(x => x.OccurredOn), context.Transactions.Max(x => x.OccurredOn)),
            ["top_categories"] = string.Join("; ", context.Dashboard.Currencies.SelectMany(x => x.Categories.Take(5)).Select(x => $"{x.Category}|{x.Amount.ToString(CultureInfo.InvariantCulture)}")),
        };
        return facts;
    }

    private static SpendingCoachResponseDto BuildCoachFallback(
        SpendingDashboardDto dashboard,
        IReadOnlyDictionary<string, decimal> evidence)
    {
        var totals = dashboard.Currencies.Select(x => $"{Money(x.Spending)} {x.Currency}").ToList();
        var topCategories = dashboard.Currencies.Where(x => x.TopCategory is not null)
            .Select(x => $"{x.Currency} için en yüksek kategori {x.TopCategory}").ToList();
        return Response(
            "Ekstre toplam harcaması: " + string.Join(", ", totals) + ".",
            topCategories,
            ["Bütçe kararından önce işlemleri ve kategori eşleşmelerini kontrol edin."],
            evidence,
            evidence.Keys.Where(x => x.StartsWith("total_spending_", StringComparison.Ordinal)).ToList(),
            false);
    }

    private static SpendingCoachResponseDto Response(
        string answer,
        IReadOnlyList<string> insights,
        IReadOnlyList<string> recommendations,
        IReadOnlyDictionary<string, decimal> evidence,
        IReadOnlyList<string> keys,
        bool aiGenerated) => new(answer, insights, recommendations, evidence, keys, Disclaimer, true, aiGenerated);

    private static SpendingCoachResponseDto NoData(string answer) =>
        new(answer, [], [], new Dictionary<string, decimal>(), [], Disclaimer, false, false);

    private static string Money(decimal value) => value.ToString("N2", new CultureInfo("tr-TR"));
    private static string CurrencyFromKey(string key) => key[(key.LastIndexOf('_') + 1)..];

    private sealed record CoachContext(
        IReadOnlyList<CoachTransaction> Transactions,
        SpendingDashboardDto Dashboard,
        SpendingPatternsDto Patterns);
    private sealed record CoachTransaction(
        DateOnly OccurredOn,
        string MerchantName,
        decimal Amount,
        string Currency,
        string Category);
}
