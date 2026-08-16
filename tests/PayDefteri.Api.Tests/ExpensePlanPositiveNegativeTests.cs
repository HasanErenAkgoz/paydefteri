using System.Net;
using PayDefteri.Api.Tests.Infrastructure;

namespace PayDefteri.Api.Tests;

[Collection("Api")]
public sealed class ExpensePlanPositiveNegativeTests
{
    private readonly ApiFixture _fixture;

    public ExpensePlanPositiveNegativeTests(ApiFixture fixture) => _fixture = fixture;

    private TestClient NewClient() => new(_fixture.Factory.CreateClient());

    [Fact]
    public async Task Positive_multi_payer_expense_and_transfer_produce_expected_net_balances()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();

        var (createPlan, plan) = await api.PostAsync<PlanDto>("/api/plans", new
        {
            title = "Ortak gider",
            description = "API coverage",
            planType = "Expense",
        });
        createPlan.EnsureSuccessStatusCode();

        var ayse = await CreatePartnerAsync(api, plan!.Id, "Ayşe", "#38bdf8", 1);
        var mehmet = await CreatePartnerAsync(api, plan.Id, "Mehmet", "#fb923c", 2);

        var (createExpense, expense) = await api.PostAsync<ExpenseDto>($"/api/plans/{plan.Id}/expenses", new
        {
            name = "Market",
            occurredOn = "2026-08-05",
            totalAmount = 300m,
            shareType = "Equal",
            status = "Paid",
            paidByPartnerId = (Guid?)null,
            categoryId = (Guid?)null,
            note = "",
            customShares = (object?)null,
            payments = new[]
            {
                new { partnerId = ayse.Id, amount = 200m },
                new { partnerId = mehmet.Id, amount = 100m },
            },
        });
        createExpense.StatusCode.Should().Be(HttpStatusCode.Created);
        expense!.TotalAmount.Should().Be(300m);

        var (_, beforeTransfer) = await api.GetAsync<ExpenseBoardDto>($"/api/plans/{plan.Id}/expenses/board");
        beforeTransfer!.Expenses.Should().ContainSingle(e => e.Id == expense.Id);
        beforeTransfer.Balances.Single(b => b.PartnerId == ayse.Id).Balance.Should().Be(50m);
        beforeTransfer.Balances.Single(b => b.PartnerId == mehmet.Id).Balance.Should().Be(-50m);

        var (transferResponse, _) = await api.PostAsync<SettlementTransferDto>($"/api/plans/{plan.Id}/expenses/transfers", new
        {
            fromPartnerId = mehmet.Id,
            toPartnerId = ayse.Id,
            amount = 50m,
            transferredOn = "2026-08-05",
            note = "Mahsup",
        });
        transferResponse.EnsureSuccessStatusCode();

        var (_, afterTransfer) = await api.GetAsync<ExpenseBoardDto>($"/api/plans/{plan.Id}/expenses/board");
        afterTransfer!.Balances.Should().OnlyContain(b => b.Balance == 0m);
        afterTransfer.Transfers.Should().ContainSingle(t => t.Amount == 50m);
    }

    [Fact]
    public async Task Positive_installment_expense_creates_monthly_rows_with_balanced_amounts_and_shares()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (createPlan, plan) = await api.PostAsync<PlanDto>("/api/plans", new
        {
            title = "Araç giderleri",
            description = "",
            planType = "Expense",
        });
        createPlan.EnsureSuccessStatusCode();

        var ayse = await CreatePartnerAsync(api, plan!.Id, "Ayşe", "#38bdf8", 1);
        var mehmet = await CreatePartnerAsync(api, plan.Id, "Mehmet", "#fb923c", 2);
        var (createExpense, _) = await api.PostAsync<ExpenseDto>($"/api/plans/{plan.Id}/expenses", new
        {
            name = "Araç ağır bakım",
            occurredOn = "2026-01-31",
            totalAmount = 1000m,
            shareType = "Custom",
            status = "Paid",
            paidByPartnerId = (Guid?)null,
            categoryId = (Guid?)null,
            note = "Kart taksiti",
            installmentCount = 3,
            customShares = new[]
            {
                new { partnerId = ayse.Id, amount = 600m },
                new { partnerId = mehmet.Id, amount = 400m },
            },
            payments = new[]
            {
                new { partnerId = ayse.Id, amount = 600m },
                new { partnerId = mehmet.Id, amount = 400m },
            },
        });
        createExpense.StatusCode.Should().Be(HttpStatusCode.Created);

        var (_, board) = await api.GetAsync<ExpenseBoardDto>($"/api/plans/{plan.Id}/expenses/board");
        var installments = board!.Expenses
            .Where(e => e.Name.StartsWith("Araç ağır bakım", StringComparison.Ordinal))
            .OrderBy(e => e.OccurredOn)
            .ToList();

        installments.Select(e => e.Name).Should().Equal(
            "Araç ağır bakım — 1. taksit",
            "Araç ağır bakım — 2. taksit",
            "Araç ağır bakım — 3. taksit");
        installments.Select(e => e.OccurredOn).Should().Equal(
            new DateOnly(2026, 1, 31),
            new DateOnly(2026, 2, 28),
            new DateOnly(2026, 3, 31));
        installments.Select(e => e.TotalAmount).Should().Equal(333.33m, 333.33m, 333.34m);
        installments.Sum(e => e.TotalAmount).Should().Be(1000m);
        installments[0].Status.ToString().Should().BeOneOf("Paid", "1");
        installments.Skip(1).Should().OnlyContain(e =>
            (e.Status.ToString() == "Planned" || e.Status.ToString() == "0") && e.Payments!.Count == 0);
        installments.Should().OnlyContain(e => e.CustomShares!.Sum(s => s.Amount) == e.TotalAmount);
        installments.Sum(e => e.CustomShares!.Single(s => s.PartnerId == ayse.Id).Amount).Should().Be(600m);
        installments.Sum(e => e.CustomShares!.Single(s => s.PartnerId == mehmet.Id).Amount).Should().Be(400m);
        installments[0].Payments!.Sum(p => p.Amount).Should().Be(333.33m);
    }

    [Fact]
    public async Task Negative_installment_plan_rejects_expense_operations()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (response, plan) = await api.PostAsync<PlanDto>("/api/plans", new { title = "Taksit", description = "" });
        response.EnsureSuccessStatusCode();

        var (boardResponse, _) = await api.GetAsync<ExpenseBoardDto>($"/api/plans/{plan!.Id}/expenses/board");
        boardResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Positive_member_can_manage_own_expense_but_not_the_owner_expense()
    {
        var owner = NewClient();
        await owner.RegisterAndLoginAsync();
        var (createPlan, plan) = await owner.PostAsync<PlanDto>("/api/plans", new
        {
            title = "Çift giderleri",
            description = "",
            planType = "Expense",
        });
        createPlan.EnsureSuccessStatusCode();
        var ownerPartner = await CreatePartnerAsync(owner, plan!.Id, "Eş 1", "#38bdf8", 1);
        var memberPartner = await CreatePartnerAsync(owner, plan.Id, "Eş 2", "#fb923c", 2);

        var (ownerCreate, ownerExpense) = await owner.PostAsync<ExpenseDto>($"/api/plans/{plan.Id}/expenses", new
        {
            name = "Ev interneti",
            occurredOn = "2026-08-05",
            totalAmount = 500m,
            shareType = "Equal",
            status = "Planned",
            paidByPartnerId = (Guid?)null,
            categoryId = (Guid?)null,
            note = "",
            customShares = (object?)null,
            payments = (object?)null,
        });
        ownerCreate.EnsureSuccessStatusCode();

        var memberEmail = $"spouse_{Guid.NewGuid():N}@example.com";
        var (inviteResponse, invite) = await owner.PostAsync<InviteCreatedDto>($"/api/plans/{plan.Id}/invites", new
        {
            email = memberEmail,
            partnerId = memberPartner.Id,
        });
        inviteResponse.EnsureSuccessStatusCode();

        var member = NewClient();
        await member.RegisterAndLoginAsync(memberEmail);
        (await member.PostAsync<PlanDto>($"/api/invites/{invite!.Token}/accept", new { })).Response.EnsureSuccessStatusCode();

        var (memberCreate, memberExpense) = await member.PostAsync<ExpenseDto>($"/api/plans/{plan.Id}/expenses", new
        {
            name = "Market",
            occurredOn = "2026-08-05",
            totalAmount = 300m,
            shareType = "Equal",
            status = "Planned",
            paidByPartnerId = (Guid?)null,
            categoryId = (Guid?)null,
            note = "",
            customShares = (object?)null,
            payments = (object?)null,
        });
        memberCreate.StatusCode.Should().Be(HttpStatusCode.Created);
        memberExpense!.CanManage.Should().BeTrue();

        var (_, board) = await member.GetAsync<ExpenseBoardDto>($"/api/plans/{plan.Id}/expenses/board");
        board!.Expenses.Single(e => e.Id == memberExpense.Id).CanManage.Should().BeTrue();
        board.Expenses.Single(e => e.Id == ownerExpense!.Id).CanManage.Should().BeFalse();

        var (forbiddenUpdate, _) = await member.PutAsync<ExpenseDto>($"/api/plans/{plan.Id}/expenses/{ownerExpense!.Id}", new
        {
            name = "Değiştirilemez",
            occurredOn = "2026-08-05",
            totalAmount = 500m,
            shareType = "Equal",
            status = "Planned",
            paidByPartnerId = (Guid?)null,
            categoryId = (Guid?)null,
            note = "",
            customShares = (object?)null,
            payments = (object?)null,
        });
        forbiddenUpdate.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var (ownUpdate, updated) = await member.PutAsync<ExpenseDto>($"/api/plans/{plan.Id}/expenses/{memberExpense.Id}", new
        {
            name = "Market haftalık",
            occurredOn = "2026-08-05",
            totalAmount = 350m,
            shareType = "Equal",
            status = "Planned",
            paidByPartnerId = (Guid?)null,
            categoryId = (Guid?)null,
            note = "",
            customShares = (object?)null,
            payments = (object?)null,
        });
        ownUpdate.EnsureSuccessStatusCode();
        updated!.Name.Should().Be("Market haftalık");
        updated.CanManage.Should().BeTrue();
    }

    private static async Task<PartnerDto> CreatePartnerAsync(TestClient api, Guid planId, string name, string color, int sortOrder)
    {
        var (response, partner) = await api.PostAsync<PartnerDto>($"/api/plans/{planId}/partners", new
        {
            name,
            color,
            defaultPct = 50m,
            sortOrder,
            iban = (string?)null,
        });
        response.EnsureSuccessStatusCode();
        return partner!;
    }
}
