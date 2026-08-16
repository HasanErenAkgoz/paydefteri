using System.Net;
using FuzulTaksitTakip.Api.Tests.Infrastructure;

namespace FuzulTaksitTakip.Api.Tests;

[Collection("Api")]
public sealed class InstallmentAbidikGubidikTests
{
    private readonly ApiFixture _fixture;
    public InstallmentAbidikGubidikTests(ApiFixture fixture) => _fixture = fixture;
    private TestClient NewClient() => new(_fixture.Factory.CreateClient());

    [Fact]
    public async Task Negative_empty_and_whitespace_installment_name_rejected()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (plan, _) = await PlanTestHelper.CreatePlanWithPartnersAsync(api);

        foreach (var name in new[] { "", "   " })
        {
            var (res, _) = await api.PostAsync<object>($"/api/plans/{plan.Id}/installments", new
            {
                name,
                dueDate = "2026-09-01",
                totalAmount = 10m,
                shareType = "Equal",
                sortOrder = 1,
                customShares = (object?)null,
            });
            res.StatusCode.Should().Be(HttpStatusCode.BadRequest, $"name='{name}'");
        }
    }

    [Fact]
    public async Task Positive_emoji_name_and_zero_amount_accepted()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (plan, _) = await PlanTestHelper.CreatePlanWithPartnersAsync(api);

        var (res, inst) = await api.PostAsync<InstallmentDto>($"/api/plans/{plan.Id}/installments", new
        {
            name = "🏠 Peşinat ₺🎉",
            dueDate = "2026-01-01",
            totalAmount = 0m,
            shareType = "Equal",
            sortOrder = 0,
            customShares = (object?)null,
        });
        res.StatusCode.Should().Be(HttpStatusCode.Created);
        inst!.Name.Should().Contain("🏠");
        inst.TotalAmount.Should().Be(0m);
    }

    [Fact]
    public async Task Negative_name_over_300_chars_and_negative_amount()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (plan, _) = await PlanTestHelper.CreatePlanWithPartnersAsync(api);

        var (longName, _) = await api.PostAsync<object>($"/api/plans/{plan.Id}/installments", new
        {
            name = new string('x', 301),
            dueDate = "2026-09-01",
            totalAmount = 1m,
            shareType = "Default",
            sortOrder = 1,
            customShares = (object?)null,
        });
        longName.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var (neg, _) = await api.PostAsync<object>($"/api/plans/{plan.Id}/installments", new
        {
            name = "Negatif",
            dueDate = "2026-09-01",
            totalAmount = -0.01m,
            shareType = "Default",
            sortOrder = 1,
            customShares = (object?)null,
        });
        neg.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Negative_invalid_share_type_string_rejected()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (plan, _) = await PlanTestHelper.CreatePlanWithPartnersAsync(api);

        var (res, _) = await api.PostAsync<object>($"/api/plans/{plan.Id}/installments", new
        {
            name = "Bad type",
            dueDate = "2026-09-01",
            totalAmount = 10m,
            shareType = "HokusPokus",
            sortOrder = 1,
            customShares = (object?)null,
        });
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Weird_custom_negative_share_that_sums_to_total_currently_accepted()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (plan, partners) = await PlanTestHelper.CreatePlanWithPartnersAsync(api);

        var (res, _) = await api.PostAsync<object>($"/api/plans/{plan.Id}/installments", new
        {
            name = "Negatif pay deliği",
            dueDate = "2026-09-01",
            totalAmount = 100m,
            shareType = "Custom",
            sortOrder = 1,
            customShares = new[]
            {
                new { partnerId = partners[0].Id, amount = -10m },
                new { partnerId = partners[1].Id, amount = 110m },
            },
        });
        // Documents current behavior: no per-share >= 0 rule
        res.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Weird_custom_share_foreign_partner_id_that_sums_ok_currently_accepted()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (plan, partners) = await PlanTestHelper.CreatePlanWithPartnersAsync(api);
        var foreign = Guid.NewGuid();

        var (res, _) = await api.PostAsync<object>($"/api/plans/{plan.Id}/installments", new
        {
            name = "Yabancı ortak payı",
            dueDate = "2026-09-01",
            totalAmount = 100m,
            shareType = "Custom",
            sortOrder = 1,
            customShares = new[]
            {
                new { partnerId = partners[0].Id, amount = 40m },
                new { partnerId = foreign, amount = 60m },
            },
        });
        // App validates share sum but not partner membership → DB FK blows up (documents gap)
        res.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Positive_switch_custom_to_equal_clears_custom_path()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (plan, partners) = await PlanTestHelper.CreatePlanWithPartnersAsync(api);
        var inst = await PlanTestHelper.CreateInstallmentAsync(
            api, plan.Id, "Custom", 100m, "Custom", 1,
            new[]
            {
                new { partnerId = partners[0].Id, amount = 60m },
                new { partnerId = partners[1].Id, amount = 40m },
            });

        var (res, updated) = await api.PutAsync<InstallmentDto>(
            $"/api/plans/{plan.Id}/installments/{inst.Id}",
            new
            {
                name = "Artık equal",
                dueDate = "2026-09-01",
                totalAmount = 100m,
                shareType = "Equal",
                sortOrder = 1,
                customShares = new[]
                {
                    new { partnerId = partners[0].Id, amount = 999m },
                },
            });
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        updated!.ShareType.Should().Be("Equal");
        updated.CustomShares.Should().BeEmpty();
    }

    [Fact]
    public async Task Negative_wrong_planId_in_url_for_installment_is_not_found()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (planA, _) = await PlanTestHelper.CreatePlanWithPartnersAsync(api);
        var (planB, _) = await PlanTestHelper.CreatePlanWithPartnersAsync(api);
        var inst = await PlanTestHelper.CreateInstallmentAsync(api, planA.Id);

        var (get, _) = await api.GetAsync<object>($"/api/plans/{planB.Id}/installments");
        get.EnsureSuccessStatusCode();

        var del = await api.DeleteAsync($"/api/plans/{planB.Id}/installments/{inst.Id}");
        del.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Positive_delete_delivery_installment_clears_delivery_id()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (plan, _) = await PlanTestHelper.CreatePlanWithPartnersAsync(api);
        var inst = await PlanTestHelper.CreateInstallmentAsync(api, plan.Id);

        var update = await PlanTestHelper.UpdatePlanRawAsync(api, plan.Id, new
        {
            title = plan.Title,
            description = "edge",
            deliveryInstallmentId = inst.Id,
            requireReceipt = false,
            ibanMode = "None",
            settlementIban = (string?)null,
            remindersEnabled = false,
            reminderDaysBefore = Array.Empty<int>(),
            reminderDaysAfter = Array.Empty<int>(),
        });
        update.EnsureSuccessStatusCode();

        (await api.DeleteAsync($"/api/plans/{plan.Id}/installments/{inst.Id}")).EnsureSuccessStatusCode();

        var (get, body) = await api.GetAsync<PlanDto>($"/api/plans/{plan.Id}");
        get.EnsureSuccessStatusCode();
        body!.DeliveryInstallmentId.Should().BeNull();
    }

    [Fact]
    public async Task Bulk_increase_rejects_zero_and_applies_only_from_sort_order()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (plan, _) = await PlanTestHelper.CreatePlanWithPartnersAsync(api);
        var early = await PlanTestHelper.CreateInstallmentAsync(api, plan.Id, "Erken", 100m, sortOrder: 1);
        var late = await PlanTestHelper.CreateInstallmentAsync(api, plan.Id, "Geç", 100m, sortOrder: 5);

        var (bad, _) = await api.PostAsync<object>($"/api/plans/{plan.Id}/installments/bulk-increase", new
        {
            fromInstallmentId = late.Id,
            type = "Fixed",
            value = 0m,
        });
        bad.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var (ok, _) = await api.PostAsync<object>($"/api/plans/{plan.Id}/installments/bulk-increase", new
        {
            fromInstallmentId = late.Id,
            type = "Fixed",
            value = 50m,
        });
        ok.EnsureSuccessStatusCode();

        var (_, list) = await api.GetAsync<List<InstallmentDto>>($"/api/plans/{plan.Id}/installments");
        list!.Single(i => i.Id == early.Id).TotalAmount.Should().Be(100m);
        list!.Single(i => i.Id == late.Id).TotalAmount.Should().Be(150m);
    }

    [Fact]
    public async Task Bulk_double_submit_compounds_amounts()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (plan, _) = await PlanTestHelper.CreatePlanWithPartnersAsync(api);
        var inst = await PlanTestHelper.CreateInstallmentAsync(api, plan.Id, total: 100m, sortOrder: 1);
        var body = new { fromInstallmentId = inst.Id, type = "Percent", value = 10m };

        (await api.PostAsync<object>($"/api/plans/{plan.Id}/installments/bulk-increase", body)).Response.EnsureSuccessStatusCode();
        (await api.PostAsync<object>($"/api/plans/{plan.Id}/installments/bulk-increase", body)).Response.EnsureSuccessStatusCode();

        var (_, list) = await api.GetAsync<List<InstallmentDto>>($"/api/plans/{plan.Id}/installments");
        // 100 * 1.1 * 1.1 = 121
        list!.Single().TotalAmount.Should().Be(121m);
    }

    [Fact]
    public async Task Negative_bulk_from_foreign_installment_not_found()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (planA, _) = await PlanTestHelper.CreatePlanWithPartnersAsync(api);
        var (planB, _) = await PlanTestHelper.CreatePlanWithPartnersAsync(api);
        var instB = await PlanTestHelper.CreateInstallmentAsync(api, planB.Id);

        var (res, _) = await api.PostAsync<object>($"/api/plans/{planA.Id}/installments/bulk-increase", new
        {
            fromInstallmentId = instB.Id,
            type = "Fixed",
            value = 1m,
        });
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
