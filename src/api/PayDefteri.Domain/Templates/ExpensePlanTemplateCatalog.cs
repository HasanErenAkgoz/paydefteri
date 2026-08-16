using PayDefteri.Domain.Entities;
using PayDefteri.Domain.Enums;

namespace PayDefteri.Domain.Templates;

public sealed record ExpenseTemplateMeta(string Key, string Title, string Description);

public sealed record ExpenseCouplePartnerSeed(string Name, string Color, decimal DefaultPct);

public sealed record ExpenseCoupleExpenseSeed(string Name, DateOnly OccurredOn, decimal TotalAmount);

public sealed record ExpenseCoupleSeedOptions(
    string? Title,
    string? Description,
    IReadOnlyList<ExpenseCouplePartnerSeed>? Partners,
    IReadOnlyList<ExpenseCoupleExpenseSeed>? Expenses = null);

public sealed record ExpenseCouplePreviewRow(
    string Name,
    DateOnly Date,
    decimal TotalAmount,
    string Kind);

public sealed record ExpenseCouplePreview(
    string Key,
    string Title,
    string Description,
    IReadOnlyList<(string Name, string Color, decimal DefaultPct)> Partners,
    IReadOnlyList<ExpenseCouplePreviewRow> Rows,
    decimal GrandTotal);

/// <summary>
/// Sample expense-plan seeds (partners, one-off expenses, recurrences, transfers).
/// Partner names are chosen at materialize time unless overridden.
/// </summary>
public static class ExpensePlanTemplateCatalog
{
    public const string CoupleKey = "couple";
    public const string TripKey = "trip";
    public const string TeamLunchKey = "teamlunch";

    private static readonly string[] MaleNames =
    [
        "Mert", "Can", "Burak", "Emre", "Kaan", "Arda", "Tolga", "Onur", "Baran", "Kerem"
    ];

    private static readonly string[] FemaleNames =
    [
        "Elif", "Zeynep", "Defne", "Melis", "Selin", "İrem", "Ece", "Naz", "Duru", "Asya"
    ];

    private static readonly string[] TeamNames =
    [
        "Ayşe", "Berk", "Cem", "Deniz", "Eda", "Furkan", "Gizem", "Hakan", "İpek", "Jale",
        "Kerem", "Leyla", "Mert", "Naz", "Ozan", "Pelin"
    ];

    private static readonly string[] Colors =
    [
        "#6366f1", "#10b981", "#f59e0b", "#0ea5e9", "#ec4899", "#a855f7", "#fb923c", "#94a3b8"
    ];

    public static readonly IReadOnlyList<string> Keys = new[] { CoupleKey, TripKey, TeamLunchKey };

    public static bool TryGetMeta(string key, out ExpenseTemplateMeta? meta)
    {
        meta = key.Trim().ToLowerInvariant() switch
        {
            CoupleKey => new ExpenseTemplateMeta(
                CoupleKey,
                "Örnek Karı-Koca Gider Planı",
                "İki ortaklı ev planı — fatura, market, tekrarlayan gider ve mahsup örneği. İsimler her seferinde rastgele."),
            TripKey => new ExpenseTemplateMeta(
                TripKey,
                "Örnek Tatil / Arkadaş Grubu",
                "Dört kişilik kısa tatil — konaklama, yemek, ulaşım. Kim ödedi / kim borçlu netleşir."),
            TeamLunchKey => new ExpenseTemplateMeta(
                TeamLunchKey,
                "Örnek Ekip Öğle Yemeği",
                "Altı kişilik ekip yemeği — tek hesap, eşit pay, yemek kartı ile ödeyen örnek. Mail listesi yerine defter."),
            _ => null
        };
        return meta is not null;
    }

    public static bool IsExpenseTemplate(string key) => TryGetMeta(key, out _);

    public static ExpenseCouplePreview BuildPreview(string key, ExpenseCoupleSeedOptions? options = null)
    {
        var normalized = key.Trim().ToLowerInvariant();
        var (title, description, partners, expenses, recurrences, transfers) =
            Materialize(normalized, Guid.Empty, new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase), options);

        var rows = new List<ExpenseCouplePreviewRow>();
        foreach (var e in expenses)
        {
            rows.Add(new ExpenseCouplePreviewRow(e.Name, e.OccurredOn, e.TotalAmount, "expense"));
        }

        foreach (var r in recurrences)
        {
            rows.Add(new ExpenseCouplePreviewRow($"{r.Name} (tekrarlayan)", r.NextOccurrence, r.TotalAmount, "recurrence"));
        }

        foreach (var t in transfers)
        {
            rows.Add(new ExpenseCouplePreviewRow(
                string.IsNullOrWhiteSpace(t.Note) ? "Mahsup transferi" : t.Note,
                t.TransferredOn,
                t.Amount,
                "transfer"));
        }

        return new ExpenseCouplePreview(
            normalized,
            title,
            description,
            partners.Select(p => (p.Name, p.Color, p.DefaultPct)).ToList(),
            rows,
            expenses.Sum(e => e.TotalAmount));
    }

    public static ExpenseCouplePreview BuildCouplePreview(ExpenseCoupleSeedOptions? options = null)
        => BuildPreview(CoupleKey, options);

    public static (
        string Title,
        string Description,
        List<Partner> Partners,
        List<Expense> Expenses,
        List<ExpenseRecurrence> Recurrences,
        List<SettlementTransfer> Transfers)
        Materialize(
            string key,
            Guid planId,
            IReadOnlyDictionary<string, Guid> categoryIdsByName,
            ExpenseCoupleSeedOptions? options = null)
        => key.Trim().ToLowerInvariant() switch
        {
            TripKey => MaterializeTrip(planId, categoryIdsByName, options),
            TeamLunchKey => MaterializeTeamLunch(planId, categoryIdsByName, options),
            _ => MaterializeCouple(planId, categoryIdsByName, options)
        };

    public static (
        string Title,
        string Description,
        List<Partner> Partners,
        List<Expense> Expenses,
        List<ExpenseRecurrence> Recurrences,
        List<SettlementTransfer> Transfers)
        MaterializeCouple(
            Guid planId,
            IReadOnlyDictionary<string, Guid> categoryIdsByName,
            ExpenseCoupleSeedOptions? options = null)
    {
        var partners = new List<Partner>();

        if (options?.Partners is { Count: > 0 })
        {
            for (var i = 0; i < options.Partners.Count; i++)
            {
                var src = options.Partners[i];
                var name = string.IsNullOrWhiteSpace(src.Name) ? $"Ortak {i + 1}" : src.Name.Trim();
                partners.Add(new Partner
                {
                    PlanId = planId,
                    Name = name,
                    Color = string.IsNullOrWhiteSpace(src.Color) ? Colors[i % Colors.Length] : src.Color.Trim(),
                    DefaultPct = src.DefaultPct > 0 ? src.DefaultPct : Math.Round(100m / options.Partners.Count, 2),
                    SortOrder = i
                });
            }
        }
        else
        {
            var male = MaleNames[Random.Shared.Next(MaleNames.Length)];
            var female = FemaleNames[Random.Shared.Next(FemaleNames.Length)];
            partners.Add(new Partner
            {
                PlanId = planId,
                Name = male,
                Color = Colors[0],
                DefaultPct = 50m,
                SortOrder = 0
            });
            partners.Add(new Partner
            {
                PlanId = planId,
                Name = female,
                Color = Colors[1],
                DefaultPct = 50m,
                SortOrder = 1
            });
        }

        var primary = partners[0];
        var secondary = partners.Count > 1 ? partners[1] : partners[0];

        Guid? Cat(string name) =>
            categoryIdsByName.TryGetValue(name, out var id) ? id : null;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        static void WithSinglePayer(Expense expense, Guid payerId)
        {
            expense.PaidByPartnerId = payerId;
            expense.Payments.Add(new ExpensePayment
            {
                ExpenseId = expense.Id,
                PartnerId = payerId,
                Amount = expense.TotalAmount,
            });
        }

        var elektrik = new Expense
        {
            PlanId = planId,
            CategoryId = Cat("Fatura"),
            Name = "Elektrik faturası",
            OccurredOn = today.AddDays(-12),
            TotalAmount = 1200m,
            ShareType = ShareType.Equal,
            Status = ExpenseStatus.Paid,
            Note = $"{primary.Name} ödedi — eşit paylaşım"
        };
        WithSinglePayer(elektrik, primary.Id);

        var market = new Expense
        {
            PlanId = planId,
            CategoryId = Cat("Market"),
            Name = "Haftalık market",
            OccurredOn = today.AddDays(-5),
            TotalAmount = 860m,
            ShareType = ShareType.Equal,
            Status = ExpenseStatus.Paid,
            Note = $"{secondary.Name} ödedi"
        };
        WithSinglePayer(market, secondary.Id);

        var yemek = new Expense
        {
            PlanId = planId,
            CategoryId = Cat("Mutfak"),
            Name = "Yemek siparişi",
            OccurredOn = today.AddDays(-3),
            TotalAmount = 420m,
            ShareType = ShareType.Equal,
            Status = ExpenseStatus.Paid,
        };
        WithSinglePayer(yemek, primary.Id);

        var uber = new Expense
        {
            PlanId = planId,
            CategoryId = Cat("Ulaşım"),
            Name = "Uber / taksi",
            OccurredOn = today.AddDays(-2),
            TotalAmount = 180m,
            ShareType = ShareType.Equal,
            Status = ExpenseStatus.Paid,
        };
        WithSinglePayer(uber, secondary.Id);

        var internet = new Expense
        {
            PlanId = planId,
            CategoryId = Cat("Fatura"),
            Name = $"İnternet ({primary.Name} kişisel)",
            OccurredOn = today.AddDays(-8),
            TotalAmount = 450m,
            ShareType = ShareType.Custom,
            Status = ExpenseStatus.Paid,
            Note = $"Sadece {primary.Name} payı — borç yok"
        };
        WithSinglePayer(internet, primary.Id);
        foreach (var p in partners)
        {
            internet.CustomShares.Add(new ExpenseShare
            {
                ExpenseId = internet.Id,
                PartnerId = p.Id,
                Amount = p.Id == primary.Id ? 450m : 0m
            });
        }

        var su = new Expense
        {
            PlanId = planId,
            CategoryId = Cat("Fatura"),
            Name = "Su faturası",
            OccurredOn = today.AddDays(5),
            TotalAmount = 340m,
            ShareType = ShareType.Equal,
            Status = ExpenseStatus.Planned,
            Note = "Gelecek dönem"
        };

        var temizlik = new Expense
        {
            PlanId = planId,
            CategoryId = Cat("Diğer"),
            Name = "Temizlik malzemesi",
            OccurredOn = today.AddDays(-1),
            TotalAmount = 250m,
            ShareType = ShareType.Default,
            Status = ExpenseStatus.Paid,
            Note = "Varsayılan paylara göre"
        };
        WithSinglePayer(temizlik, primary.Id);

        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var recurrence = new ExpenseRecurrence
        {
            PlanId = planId,
            CategoryId = Cat("Fatura"),
            Name = "Aylık elektrik",
            TotalAmount = 1100m,
            ShareType = ShareType.Equal,
            DefaultPaidByPartnerId = primary.Id,
            Frequency = RecurrenceFrequency.Monthly,
            AnchorDay = 15,
            StartDate = monthStart,
            NextOccurrence = monthStart.AddDays(14),
            IsActive = true,
            Note = "Her ay 15'inde otomatik oluşur"
        };

        var transfers = new List<SettlementTransfer>();
        if (partners.Count >= 2)
        {
            transfers.Add(new SettlementTransfer
            {
                PlanId = planId,
                FromPartnerId = secondary.Id,
                ToPartnerId = primary.Id,
                Amount = 400m,
                TransferredOn = today.AddDays(-1),
                Note = $"Örnek mahsup — {secondary.Name} → {primary.Name}"
            });
        }

        var names = string.Join(" & ", partners.Select(p => p.Name));
        var title = string.IsNullOrWhiteSpace(options?.Title) ? "Ev Ortak Giderleri" : options!.Title.Trim();
        var description = string.IsNullOrWhiteSpace(options?.Description)
            ? $"Örnek: {names} ortak fatura / market takibi"
            : options!.Description.Trim();

        if (options?.Expenses is { Count: > 0 })
        {
            var customExpenses = new List<Expense>();
            for (var i = 0; i < options.Expenses.Count; i++)
            {
                var row = options.Expenses[i];
                var name = string.IsNullOrWhiteSpace(row.Name) ? $"{i + 1}. Gider" : row.Name.Trim();
                var amount = row.TotalAmount < 0 ? 0 : row.TotalAmount;
                var payer = partners[i % partners.Count];
                var planned = row.OccurredOn > today;
                var expense = new Expense
                {
                    PlanId = planId,
                    CategoryId = Cat("Diğer"),
                    Name = name,
                    OccurredOn = row.OccurredOn,
                    TotalAmount = amount,
                    ShareType = ShareType.Equal,
                    Status = planned ? ExpenseStatus.Planned : ExpenseStatus.Paid,
                    Note = planned ? "Önizlemeden — planlandı" : "Önizlemeden"
                };
                if (!planned && amount > 0m)
                {
                    WithSinglePayer(expense, payer.Id);
                }

                customExpenses.Add(expense);
            }

            return (title, description, partners, customExpenses, [], []);
        }

        return (
            title,
            description,
            partners,
            [elektrik, market, yemek, uber, internet, su, temizlik],
            [recurrence],
            transfers);
    }

    private static (
        string Title,
        string Description,
        List<Partner> Partners,
        List<Expense> Expenses,
        List<ExpenseRecurrence> Recurrences,
        List<SettlementTransfer> Transfers)
        MaterializeTrip(
            Guid planId,
            IReadOnlyDictionary<string, Guid> categoryIdsByName,
            ExpenseCoupleSeedOptions? options)
    {
        var partners = BuildPartners(planId, options, defaultCount: 4, namePool: null);
        Guid? Cat(string name) =>
            categoryIdsByName.TryGetValue(name, out var id) ? id : null;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (options?.Expenses is { Count: > 0 })
        {
            return ApplyCustomExpenses(
                planId,
                partners,
                options,
                Cat,
                today,
                defaultTitle: "Tatil Harcama Defteri",
                defaultDescription: $"Örnek tatil — {string.Join(", ", partners.Select(p => p.Name))}");
        }

        var a = partners[0];
        var b = partners[1];
        var c = partners.Count > 2 ? partners[2] : partners[0];
        var d = partners.Count > 3 ? partners[3] : partners[1];

        var hotel = PaidExpense(planId, Cat("Diğer"), "Konaklama (2 gece)", today.AddDays(-3), 4800m, a.Id,
            $"{a.Name} kartla ödedi — eşit pay");
        var dinner = PaidExpense(planId, Cat("Mutfak"), "Akşam yemeği", today.AddDays(-2), 1600m, b.Id,
            $"{b.Name} ödedi");
        var taxi = PaidExpense(planId, Cat("Ulaşım"), "Havalimanı transferi", today.AddDays(-3), 900m, c.Id, null);
        var museum = PaidExpense(planId, Cat("Diğer"), "Müze / aktivite", today.AddDays(-1), 800m, d.Id, null);
        var breakfast = new Expense
        {
            PlanId = planId,
            CategoryId = Cat("Mutfak"),
            Name = "Kahvaltı (planlandı)",
            OccurredOn = today.AddDays(1),
            TotalAmount = 600m,
            ShareType = ShareType.Equal,
            Status = ExpenseStatus.Planned,
            Note = "Henüz ödenmedi"
        };

        var transfers = new List<SettlementTransfer>();
        if (partners.Count >= 2)
        {
            transfers.Add(new SettlementTransfer
            {
                PlanId = planId,
                FromPartnerId = b.Id,
                ToPartnerId = a.Id,
                Amount = 500m,
                TransferredOn = today,
                Note = $"Örnek mahsup — {b.Name} → {a.Name}"
            });
        }

        var names = string.Join(", ", partners.Select(p => p.Name));
        var title = string.IsNullOrWhiteSpace(options?.Title) ? "Tatil Harcama Defteri" : options!.Title.Trim();
        var description = string.IsNullOrWhiteSpace(options?.Description)
            ? $"Örnek tatil grubu: {names}"
            : options!.Description.Trim();

        return (title, description, partners, [hotel, dinner, taxi, museum, breakfast], [], transfers);
    }

    private static (
        string Title,
        string Description,
        List<Partner> Partners,
        List<Expense> Expenses,
        List<ExpenseRecurrence> Recurrences,
        List<SettlementTransfer> Transfers)
        MaterializeTeamLunch(
            Guid planId,
            IReadOnlyDictionary<string, Guid> categoryIdsByName,
            ExpenseCoupleSeedOptions? options)
    {
        var partners = BuildPartners(planId, options, defaultCount: 6, namePool: TeamNames);
        Guid? Cat(string name) =>
            categoryIdsByName.TryGetValue(name, out var id) ? id : null;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (options?.Expenses is { Count: > 0 })
        {
            return ApplyCustomExpenses(
                planId,
                partners,
                options,
                Cat,
                today,
                defaultTitle: "Ekip Öğle Yemeği",
                defaultDescription: "Örnek ekip yemeği — eşit pay, tek ödeyen");
        }

        var payer = partners[0];
        var lunch = PaidExpense(
            planId,
            Cat("Mutfak"),
            "Ekip öğle yemeği",
            today.AddDays(-1),
            2400m,
            payer.Id,
            $"{payer.Name} yemek kartıyla ödedi — eşit pay (mail listesi yok)");
        var tip = PaidExpense(
            planId,
            Cat("Diğer"),
            "Bahşiş / servis",
            today.AddDays(-1),
            240m,
            partners[1].Id,
            $"{partners[1].Name} nakit ödedi");

        var transfers = new List<SettlementTransfer>
        {
            new()
            {
                PlanId = planId,
                FromPartnerId = partners[2].Id,
                ToPartnerId = payer.Id,
                Amount = 400m,
                TransferredOn = today,
                Note = $"{partners[2].Name} → {payer.Name} (yemek kartı mahsup)"
            }
        };

        var names = string.Join(", ", partners.Select(p => p.Name));
        var title = string.IsNullOrWhiteSpace(options?.Title) ? "Ekip Öğle Yemeği" : options!.Title.Trim();
        var description = string.IsNullOrWhiteSpace(options?.Description)
            ? $"Örnek ekip hesabı ({partners.Count} kişi): {names}"
            : options!.Description.Trim();

        return (title, description, partners, [lunch, tip], [], transfers);
    }

    private static List<Partner> BuildPartners(
        Guid planId,
        ExpenseCoupleSeedOptions? options,
        int defaultCount,
        string[]? namePool)
    {
        var partners = new List<Partner>();
        if (options?.Partners is { Count: > 0 })
        {
            for (var i = 0; i < options.Partners.Count; i++)
            {
                var src = options.Partners[i];
                var name = string.IsNullOrWhiteSpace(src.Name) ? $"Ortak {i + 1}" : src.Name.Trim();
                partners.Add(new Partner
                {
                    PlanId = planId,
                    Name = name,
                    Color = string.IsNullOrWhiteSpace(src.Color) ? Colors[i % Colors.Length] : src.Color.Trim(),
                    DefaultPct = src.DefaultPct > 0 ? src.DefaultPct : Math.Round(100m / options.Partners.Count, 2),
                    SortOrder = i
                });
            }

            return partners;
        }

        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < defaultCount; i++)
        {
            string name;
            if (namePool is { Length: > 0 })
            {
                do
                {
                    name = namePool[Random.Shared.Next(namePool.Length)];
                } while (!used.Add(name) && used.Count < namePool.Length);
            }
            else
            {
                var pool = i % 2 == 0 ? MaleNames : FemaleNames;
                do
                {
                    name = pool[Random.Shared.Next(pool.Length)];
                } while (!used.Add(name) && used.Count < MaleNames.Length + FemaleNames.Length);
            }

            partners.Add(new Partner
            {
                PlanId = planId,
                Name = name,
                Color = Colors[i % Colors.Length],
                DefaultPct = Math.Round(100m / defaultCount, 2),
                SortOrder = i
            });
        }

        // Fix rounding so pcts sum ~100
        var sum = partners.Sum(p => p.DefaultPct);
        if (partners.Count > 0 && Math.Abs(sum - 100m) > 0.01m)
        {
            partners[^1].DefaultPct += 100m - sum;
        }

        return partners;
    }

    private static Expense PaidExpense(
        Guid planId,
        Guid? categoryId,
        string name,
        DateOnly occurredOn,
        decimal amount,
        Guid payerId,
        string? note)
    {
        var expense = new Expense
        {
            PlanId = planId,
            CategoryId = categoryId,
            Name = name,
            OccurredOn = occurredOn,
            TotalAmount = amount,
            ShareType = ShareType.Equal,
            Status = ExpenseStatus.Paid,
            Note = note ?? string.Empty,
            PaidByPartnerId = payerId
        };
        expense.Payments.Add(new ExpensePayment
        {
            ExpenseId = expense.Id,
            PartnerId = payerId,
            Amount = amount
        });
        return expense;
    }

    private static (
        string Title,
        string Description,
        List<Partner> Partners,
        List<Expense> Expenses,
        List<ExpenseRecurrence> Recurrences,
        List<SettlementTransfer> Transfers)
        ApplyCustomExpenses(
            Guid planId,
            List<Partner> partners,
            ExpenseCoupleSeedOptions options,
            Func<string, Guid?> cat,
            DateOnly today,
            string defaultTitle,
            string defaultDescription)
    {
        var title = string.IsNullOrWhiteSpace(options.Title) ? defaultTitle : options.Title.Trim();
        var description = string.IsNullOrWhiteSpace(options.Description)
            ? defaultDescription
            : options.Description.Trim();
        var customExpenses = new List<Expense>();
        for (var i = 0; i < options.Expenses!.Count; i++)
        {
            var row = options.Expenses[i];
            var name = string.IsNullOrWhiteSpace(row.Name) ? $"{i + 1}. Gider" : row.Name.Trim();
            var amount = row.TotalAmount < 0 ? 0 : row.TotalAmount;
            var payer = partners[i % partners.Count];
            var planned = row.OccurredOn > today;
            var expense = new Expense
            {
                PlanId = planId,
                CategoryId = cat("Diğer"),
                Name = name,
                OccurredOn = row.OccurredOn,
                TotalAmount = amount,
                ShareType = ShareType.Equal,
                Status = planned ? ExpenseStatus.Planned : ExpenseStatus.Paid,
                Note = planned ? "Önizlemeden — planlandı" : "Önizlemeden"
            };
            if (!planned && amount > 0m)
            {
                expense.PaidByPartnerId = payer.Id;
                expense.Payments.Add(new ExpensePayment
                {
                    ExpenseId = expense.Id,
                    PartnerId = payer.Id,
                    Amount = amount
                });
            }

            customExpenses.Add(expense);
        }

        return (title, description, partners, customExpenses, [], []);
    }
}
