using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PayDefteri.Application.Common.Exceptions;
using PayDefteri.Application.Common.Interfaces;
using PayDefteri.Domain.Entities;
using PayDefteri.Domain.Templates;

namespace PayDefteri.Application.SpendingAnalysis;

public sealed record SpendingCategoryMetricDto(string Category, decimal Amount, int TransactionCount, decimal Percentage);
public sealed record SpendingDailyMetricDto(DateOnly Date, decimal Amount);
public sealed record LargestPurchaseDto(DateOnly Date, string MerchantName, decimal Amount);
public sealed record SpendingCurrencyDashboardDto(string Currency, decimal Spending, int TransactionCount,
    string? TopCategory, LargestPurchaseDto? LargestPurchase, decimal DailyAverage, decimal NecessityRatio,
    IReadOnlyList<SpendingCategoryMetricDto> Categories, IReadOnlyList<SpendingDailyMetricDto> DailySeries);
public sealed record SpendingDashboardDto(Guid StatementId, IReadOnlyList<SpendingCurrencyDashboardDto> Currencies);

public sealed record GetSpendingDashboardQuery(Guid StatementId) : IRequest<SpendingDashboardDto>;
public sealed class GetSpendingDashboardQueryHandler : IRequestHandler<GetSpendingDashboardQuery, SpendingDashboardDto>
{
    private static readonly HashSet<string> Necessities =
    [
        "Market", "Sağlık", "Eğitim", "Ulaşım / Taksi", "Akaryakıt",
        "Telefon / İnternet", "Elektrik / Su / Doğalgaz", "Sigorta", "Vergi",
    ];
    private readonly IAppDbContext _db; private readonly ICurrentUser _user;
    public GetSpendingDashboardQueryHandler(IAppDbContext db, ICurrentUser user) { _db = db; _user = user; }
    public async Task<SpendingDashboardDto> Handle(GetSpendingDashboardQuery request, CancellationToken ct)
    {
        var owner = UploadSpendingStatementCommandHandler.RequireUserId(_user);
        var statement = await _db.SpendingStatements.AsNoTracking().Include(x => x.Transactions)
            .SingleOrDefaultAsync(x => x.Id == request.StatementId && x.OwnerUserId == owner, ct)
            ?? throw new NotFoundException("Ekstre bulunamadı.");
        var days = Math.Max(1, (statement.PeriodEnd!.Value.DayNumber - statement.PeriodStart!.Value.DayNumber) + 1);
        var currencies = statement.Transactions.Where(x => !x.IsRefund).GroupBy(x => x.Currency).OrderBy(x => x.Key)
            .Select(group =>
            {
                var total = group.Sum(x => x.Amount);
                var categories = group.GroupBy(x => x.Category).Select(x => new SpendingCategoryMetricDto(
                    x.Key, x.Sum(y => y.Amount), x.Count(), total == 0 ? 0 : Math.Round(x.Sum(y => y.Amount) / total * 100, 2)))
                    .OrderByDescending(x => x.Amount).ThenBy(x => x.Category).ToList();
                var largest = group.OrderByDescending(x => x.Amount).ThenBy(x => x.OccurredOn).First();
                return new SpendingCurrencyDashboardDto(group.Key, total, group.Count(), categories.FirstOrDefault()?.Category,
                    new(largest.OccurredOn, largest.MerchantName, largest.Amount), Math.Round(total / days, 2),
                    total == 0 ? 0 : Math.Round(group.Where(x => Necessities.Contains(x.Category)).Sum(x => x.Amount) / total * 100, 2),
                    categories, group.GroupBy(x => x.OccurredOn).OrderBy(x => x.Key)
                        .Select(x => new SpendingDailyMetricDto(x.Key, x.Sum(y => y.Amount))).ToList());
            }).ToList();
        return new(statement.Id, currencies);
    }
}

public sealed record CurrencyComparisonDto(string Currency, decimal CurrentSpending, decimal PreviousSpending,
    decimal ChangeAmount, decimal? ChangePercentage);
public sealed record StatementComparisonDto(Guid PreviousStatementId, IReadOnlyList<CurrencyComparisonDto> Currencies);
public sealed record MerchantPatternDto(string Currency, string MerchantName, int TransactionCount, decimal TotalAmount);
public sealed record CurrencyActivityDto(string Currency, decimal WeekdaySpending, decimal WeekendSpending,
    SpendingDailyMetricDto? TopDay, IReadOnlyList<MerchantPatternDto> TopMerchants);
public sealed record SpendingPatternsDto(Guid StatementId, StatementComparisonDto? PreviousStatementComparison,
    IReadOnlyList<MerchantPatternDto> RecurringCandidates, IReadOnlyList<MerchantPatternDto> SmallFrequentClusters,
    IReadOnlyList<CurrencyActivityDto> Activity, IReadOnlyList<string> PositiveFindings);
public sealed record GetSpendingPatternsQuery(Guid StatementId) : IRequest<SpendingPatternsDto>;

public sealed class GetSpendingPatternsQueryHandler : IRequestHandler<GetSpendingPatternsQuery, SpendingPatternsDto>
{
    private readonly IAppDbContext _db; private readonly ICurrentUser _user;
    public GetSpendingPatternsQueryHandler(IAppDbContext db, ICurrentUser user) { _db = db; _user = user; }
    public async Task<SpendingPatternsDto> Handle(GetSpendingPatternsQuery request, CancellationToken ct)
    {
        var owner = UploadSpendingStatementCommandHandler.RequireUserId(_user);
        var current = await _db.SpendingStatements.AsNoTracking().Include(x => x.Transactions)
            .SingleOrDefaultAsync(x => x.Id == request.StatementId && x.OwnerUserId == owner, ct)
            ?? throw new NotFoundException("Ekstre bulunamadı.");
        var previous = await _db.SpendingStatements.AsNoTracking().Include(x => x.Transactions)
            .Where(x => x.OwnerUserId == owner && x.Id != current.Id && x.PeriodEnd < current.PeriodStart)
            .OrderByDescending(x => x.PeriodEnd).FirstOrDefaultAsync(ct);
        var spend = current.Transactions.Where(x => !x.IsRefund).ToList();
        StatementComparisonDto? comparison = null;
        if (previous is not null)
        {
            var old = previous.Transactions.Where(x => !x.IsRefund).GroupBy(x => x.Currency).ToDictionary(x => x.Key, x => x.Sum(y => y.Amount));
            var compared = spend.GroupBy(x => x.Currency).Where(x => old.ContainsKey(x.Key)).Select(x =>
            {
                var now = x.Sum(y => y.Amount); var before = old[x.Key];
                return new CurrencyComparisonDto(x.Key, now, before, now - before,
                    before == 0 ? null : Math.Round((now - before) / before * 100, 2));
            }).OrderBy(x => x.Currency).ToList();
            comparison = new(previous.Id, compared);
        }
        var recurring = previous is null ? [] : spend.GroupBy(x => new { x.Currency, Key = MerchantPreferenceKey.From(x.MerchantName) })
            .Where(x => previous.Transactions.Any(y => !y.IsRefund && y.Currency == x.Key.Currency && MerchantPreferenceKey.From(y.MerchantName) == x.Key.Key))
            .Select(x => ToMerchant(x.Key.Currency, x)).OrderByDescending(x => x.TotalAmount).ToList();
        var small = spend.GroupBy(x => new { x.Currency, Key = MerchantPreferenceKey.From(x.MerchantName) })
            .Where(x => x.Count() >= 3 && x.Average(y => y.Amount) <= SmallPurchaseThreshold(x.Key.Currency))
            .Select(x => ToMerchant(x.Key.Currency, x)).ToList();
        var activity = spend.GroupBy(x => x.Currency).OrderBy(x => x.Key).Select(group =>
        {
            var daily = group.GroupBy(x => x.OccurredOn).Select(x => new SpendingDailyMetricDto(x.Key, x.Sum(y => y.Amount))).OrderByDescending(x => x.Amount).ToList();
            return new CurrencyActivityDto(group.Key,
                group.Where(x => x.OccurredOn.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday).Sum(x => x.Amount),
                group.Where(x => x.OccurredOn.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday).Sum(x => x.Amount),
                daily.FirstOrDefault(), group.GroupBy(x => new { x.Currency, Key = MerchantPreferenceKey.From(x.MerchantName) })
                    .Select(x => ToMerchant(x.Key.Currency, x)).OrderByDescending(x => x.TotalAmount).Take(5).ToList());
        }).ToList();
        var findings = comparison?.Currencies.Where(x => x.ChangeAmount < 0)
            .Select(x => $"{x.Currency} harcaması önceki ekstreye göre {Math.Abs(x.ChangeAmount):0.00} azaldı.").ToList() ?? [];
        return new(current.Id, comparison, recurring, small, activity, findings);
    }
    private static MerchantPatternDto ToMerchant(string currency, IEnumerable<SpendingTransaction> transactions)
    {
        var items = transactions.ToList();
        return new(currency, items[0].MerchantName, items.Count, items.Sum(x => x.Amount));
    }
    private static decimal SmallPurchaseThreshold(string currency) => currency == "TRY" ? 100m : 10m;
}

public sealed record SpendingBudgetDto(Guid Id, DateOnly Month, string Category, string Currency, decimal Amount);
public sealed record ListSpendingBudgetsQuery(DateOnly Month) : IRequest<IReadOnlyList<SpendingBudgetDto>>;
public sealed record UpsertSpendingBudgetCommand(DateOnly Month, string Category, string Currency, decimal Amount) : IRequest<SpendingBudgetDto>;
public sealed record DeleteSpendingBudgetCommand(DateOnly Month, string Category, string Currency) : IRequest;
public sealed class UpsertSpendingBudgetCommandValidator : AbstractValidator<UpsertSpendingBudgetCommand>
{
    public UpsertSpendingBudgetCommandValidator() { RuleFor(x => x.Category).Must(x => SpendingCategoryCatalog.All.Contains(x)); RuleFor(x => x.Currency).Matches("^[A-Z]{3}$"); RuleFor(x => x.Amount).GreaterThan(0); }
}
public sealed record BudgetCategoryStatusDto(string Category, string Currency, decimal Budget, decimal Spent,
    decimal Projected, int HealthScore, string Explanation);
public sealed record SpendingBudgetStatusDto(Guid StatementId, IReadOnlyList<BudgetCategoryStatusDto> Categories);
public sealed record GetSpendingBudgetStatusQuery(Guid StatementId) : IRequest<SpendingBudgetStatusDto>;

public sealed class SpendingBudgetHandlers :
    IRequestHandler<ListSpendingBudgetsQuery, IReadOnlyList<SpendingBudgetDto>>,
    IRequestHandler<UpsertSpendingBudgetCommand, SpendingBudgetDto>, IRequestHandler<DeleteSpendingBudgetCommand>,
    IRequestHandler<GetSpendingBudgetStatusQuery, SpendingBudgetStatusDto>
{
    private readonly IAppDbContext _db; private readonly ICurrentUser _user;
    public SpendingBudgetHandlers(IAppDbContext db, ICurrentUser user) { _db = db; _user = user; }
    private string Owner => UploadSpendingStatementCommandHandler.RequireUserId(_user);
    public async Task<IReadOnlyList<SpendingBudgetDto>> Handle(ListSpendingBudgetsQuery r, CancellationToken ct) =>
        (await _db.SpendingCategoryBudgets.AsNoTracking().Where(x => x.OwnerUserId == Owner && x.Month == r.Month).OrderBy(x => x.Currency).ThenBy(x => x.Category).ToListAsync(ct)).Select(Map).ToList();
    public async Task<SpendingBudgetDto> Handle(UpsertSpendingBudgetCommand r, CancellationToken ct)
    {
        var currency = r.Currency.ToUpperInvariant();
        var item = await _db.SpendingCategoryBudgets.SingleOrDefaultAsync(x => x.OwnerUserId == Owner && x.Month == r.Month && x.Category == r.Category && x.Currency == currency, ct);
        if (item is null) { item = new() { OwnerUserId = Owner, Month = r.Month, Category = r.Category, Currency = currency }; _db.SpendingCategoryBudgets.Add(item); }
        item.Amount = r.Amount; await _db.SaveChangesAsync(ct); return Map(item);
    }
    public async Task Handle(DeleteSpendingBudgetCommand r, CancellationToken ct)
    {
        var item = await _db.SpendingCategoryBudgets.SingleOrDefaultAsync(x => x.OwnerUserId == Owner && x.Month == r.Month && x.Category == r.Category && x.Currency == r.Currency, ct) ?? throw new NotFoundException("Bütçe bulunamadı.");
        _db.SpendingCategoryBudgets.Remove(item); await _db.SaveChangesAsync(ct);
    }
    public async Task<SpendingBudgetStatusDto> Handle(GetSpendingBudgetStatusQuery r, CancellationToken ct)
    {
        var statement = await _db.SpendingStatements.AsNoTracking().Include(x => x.Transactions).SingleOrDefaultAsync(x => x.Id == r.StatementId && x.OwnerUserId == Owner, ct) ?? throw new NotFoundException("Ekstre bulunamadı.");
        var month = new DateOnly(statement.PeriodEnd!.Value.Year, statement.PeriodEnd.Value.Month, 1);
        var budgets = await _db.SpendingCategoryBudgets.AsNoTracking().Where(x => x.OwnerUserId == Owner && x.Month == month).ToListAsync(ct);
        var observedDays = Math.Max(1, statement.PeriodEnd.Value.Day); var monthDays = DateTime.DaysInMonth(month.Year, month.Month);
        var result = budgets.Select(b => { var spent = statement.Transactions.Where(x => !x.IsRefund && x.Category == b.Category && x.Currency == b.Currency).Sum(x => x.Amount); var projected = Math.Round(spent / observedDays * monthDays, 2); var ratio = b.Amount == 0 ? 1 : projected / b.Amount; var score = Math.Clamp((int)Math.Round(100 - Math.Max(0, ratio - 0.5m) * 100), 0, 100); return new BudgetCategoryStatusDto(b.Category, b.Currency, b.Amount, spent, projected, score, $"{observedDays} günlük {spent:0.00} harcamaya göre ay sonu tahmini {projected:0.00} {b.Currency}; bütçe {b.Amount:0.00} {b.Currency}."); }).ToList();
        return new(statement.Id, result);
    }
    private static SpendingBudgetDto Map(SpendingCategoryBudget x) => new(x.Id, x.Month, x.Category, x.Currency, x.Amount);
}
