using System.Net;
using PayDefteri.Api.Tests.Infrastructure;

namespace PayDefteri.Api.Tests;

[Collection("Api")]
public sealed class InstallmentPaymentPositiveNegativeTests
{
    private readonly ApiFixture _fixture;

    public InstallmentPaymentPositiveNegativeTests(ApiFixture fixture) => _fixture = fixture;

    private TestClient NewClient() => new(_fixture.Factory.CreateClient());

    [Fact]
    public async Task Positive_Owner_marks_partner_payment_as_paid()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();

        var plan = await CreateEmptyPlanAsync(api);
        var partners = await ListPartnersAsync(api, plan.Id);
        partners.Should().HaveCountGreaterThanOrEqualTo(2);

        var due = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).ToString("yyyy-MM-dd");
        var (createInst, installment) = await api.PostAsync<InstallmentDto>(
            $"/api/plans/{plan.Id}/installments",
            new
            {
                name = "Taksit 1",
                dueDate = due,
                totalAmount = 10_000m,
                shareType = "Default",
                sortOrder = 1,
                customShares = (object?)null,
            });
        createInst.EnsureSuccessStatusCode();

        var partnerId = partners[0].Id;
        var (pay, payment) = await api.PutAsync<PaymentDto>(
            $"/api/plans/{plan.Id}/installments/{installment!.Id}/payments/{partnerId}",
            new
            {
                isPaid = true,
                paidAt = due,
                paidByPartnerId = (Guid?)null,
                note = "Ödendi",
            });
        pay.StatusCode.Should().Be(HttpStatusCode.OK);
        payment!.IsPaid.Should().BeTrue();
    }

    [Fact]
    public async Task Negative_Cannot_mark_payment_for_future_month()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();

        var plan = await CreateEmptyPlanAsync(api);
        var partners = await ListPartnersAsync(api, plan.Id);

        var future = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(2);
        var (createInst, installment) = await api.PostAsync<InstallmentDto>(
            $"/api/plans/{plan.Id}/installments",
            new
            {
                name = "İleri taksit",
                dueDate = future.ToString("yyyy-MM-dd"),
                totalAmount = 1000m,
                shareType = "Default",
                sortOrder = 1,
                customShares = (object?)null,
            });
        createInst.EnsureSuccessStatusCode();

        var (pay, _) = await api.PutAsync<object>(
            $"/api/plans/{plan.Id}/installments/{installment!.Id}/payments/{partners[0].Id}",
            new
            {
                isPaid = true,
                paidAt = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
                paidByPartnerId = (Guid?)null,
                note = "erken",
            });
        pay.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Negative_Custom_shares_not_matching_total_is_bad_request()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();

        var plan = await CreateEmptyPlanAsync(api);
        var partners = await ListPartnersAsync(api, plan.Id);
        partners.Should().HaveCountGreaterThanOrEqualTo(2);

        var (response, _) = await api.PostAsync<object>(
            $"/api/plans/{plan.Id}/installments",
            new
            {
                name = "Hatalı özel pay",
                dueDate = "2026-09-01",
                totalAmount = 100m,
                shareType = "Custom",
                sortOrder = 1,
                customShares = new[]
                {
                    new { partnerId = partners[0].Id, amount = 60m },
                    new { partnerId = partners[1].Id, amount = 30m },
                },
            });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Positive_Custom_shares_matching_total_is_accepted()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();

        var plan = await CreateEmptyPlanAsync(api);
        var partners = await ListPartnersAsync(api, plan.Id);

        var (response, installment) = await api.PostAsync<InstallmentDto>(
            $"/api/plans/{plan.Id}/installments",
            new
            {
                name = "Doğru özel pay",
                dueDate = "2026-09-01",
                totalAmount = 100m,
                shareType = "Custom",
                sortOrder = 1,
                customShares = new[]
                {
                    new { partnerId = partners[0].Id, amount = 60m },
                    new { partnerId = partners[1].Id, amount = 40m },
                },
            });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        installment!.CustomShares.Sum(s => s.Amount).Should().Be(100m);
    }

    [Fact]
    public async Task Negative_Other_user_cannot_upsert_payment_on_foreign_plan()
    {
        var owner = NewClient();
        await owner.RegisterAndLoginAsync();
        var plan = await CreateEmptyPlanAsync(owner);
        var partners = await ListPartnersAsync(owner, plan.Id);

        var (createInst, installment) = await owner.PostAsync<InstallmentDto>(
            $"/api/plans/{plan.Id}/installments",
            new
            {
                name = "T1",
                dueDate = "2026-09-01",
                totalAmount = 1000m,
                shareType = "Equal",
                sortOrder = 1,
                customShares = (object?)null,
            });
        createInst.EnsureSuccessStatusCode();

        var stranger = NewClient();
        await stranger.RegisterAndLoginAsync();
        var (pay, _) = await stranger.PutAsync<object>(
            $"/api/plans/{plan.Id}/installments/{installment!.Id}/payments/{partners[0].Id}",
            new
            {
                isPaid = true,
                paidAt = "2026-08-01",
                paidByPartnerId = (Guid?)null,
                note = "hack",
            });
        pay.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    private static async Task<PlanDto> CreateEmptyPlanAsync(TestClient api)
    {
        var (create, plan) = await api.PostAsync<PlanDto>("/api/plans", new
        {
            title = $"Plan {Guid.NewGuid():N}",
            description = "test",
        });
        create.EnsureSuccessStatusCode();
        (await api.PostEmptyAsync($"/api/plans/{plan!.Id}/seed/empty")).EnsureSuccessStatusCode();

        await api.PostAsync<PartnerDto>($"/api/plans/{plan.Id}/partners", new
        {
            name = "A",
            color = "#38bdf8",
            defaultPct = 50m,
            sortOrder = 1,
            iban = (string?)null,
        });
        await api.PostAsync<PartnerDto>($"/api/plans/{plan.Id}/partners", new
        {
            name = "B",
            color = "#fb923c",
            defaultPct = 50m,
            sortOrder = 2,
            iban = (string?)null,
        });

        return plan;
    }

    private static async Task<List<PartnerDto>> ListPartnersAsync(TestClient api, Guid planId)
    {
        var (response, partners) = await api.GetAsync<List<PartnerDto>>($"/api/plans/{planId}/partners");
        response.EnsureSuccessStatusCode();
        return partners!;
    }
}
