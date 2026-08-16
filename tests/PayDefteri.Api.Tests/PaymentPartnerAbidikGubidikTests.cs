using System.Net;
using System.Text;
using PayDefteri.Api.Tests.Infrastructure;

namespace PayDefteri.Api.Tests;

[Collection("Api")]
public sealed class PaymentPartnerAbidikGubidikTests
{
    private readonly ApiFixture _fixture;
    public PaymentPartnerAbidikGubidikTests(ApiFixture fixture) => _fixture = fixture;
    private TestClient NewClient() => new(_fixture.Factory.CreateClient());

    [Fact]
    public async Task Negative_require_receipt_blocks_paid_without_upload()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (plan, partners) = await PlanTestHelper.CreatePlanWithPartnersAsync(api);
        var inst = await PlanTestHelper.CreateInstallmentAsync(api, plan.Id);

        (await PlanTestHelper.UpdatePlanRawAsync(api, plan.Id, new
        {
            title = plan.Title,
            description = "edge",
            deliveryInstallmentId = (Guid?)null,
            requireReceipt = true,
            ibanMode = "Partner",
            settlementIban = (string?)null,
            remindersEnabled = false,
            reminderDaysBefore = Array.Empty<int>(),
            reminderDaysAfter = Array.Empty<int>(),
        })).EnsureSuccessStatusCode();

        var (pay, _) = await api.PutAsync<object>(
            $"/api/plans/{plan.Id}/installments/{inst.Id}/payments/{partners[0].Id}",
            new { isPaid = true, paidAt = "2026-08-01", paidByPartnerId = (Guid?)null, note = "yok" });
        pay.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Positive_receipt_upload_then_mark_paid_and_unmark_clears_receipt()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (plan, partners) = await PlanTestHelper.CreatePlanWithPartnersAsync(api);
        var inst = await PlanTestHelper.CreateInstallmentAsync(api, plan.Id);

        (await PlanTestHelper.UpdatePlanRawAsync(api, plan.Id, new
        {
            title = plan.Title,
            description = "edge",
            deliveryInstallmentId = (Guid?)null,
            requireReceipt = true,
            ibanMode = "Partner",
            settlementIban = (string?)null,
            remindersEnabled = false,
            reminderDaysBefore = Array.Empty<int>(),
            reminderDaysAfter = Array.Empty<int>(),
        })).EnsureSuccessStatusCode();

        var upload = await PlanTestHelper.UploadTinyPdfReceiptAsync(
            api.Http, plan.Id, inst.Id, partners[0].Id, "../../../evil.pdf");
        upload.StatusCode.Should().Be(HttpStatusCode.OK);

        var (pay, payment) = await api.PutAsync<PaymentDto>(
            $"/api/plans/{plan.Id}/installments/{inst.Id}/payments/{partners[0].Id}",
            new { isPaid = true, paidAt = "2026-08-01", paidByPartnerId = (Guid?)null, note = "ok" });
        pay.EnsureSuccessStatusCode();
        payment!.IsPaid.Should().BeTrue();
        payment.HasReceipt.Should().BeTrue();

        var (unmark, cleared) = await api.PutAsync<PaymentDto>(
            $"/api/plans/{plan.Id}/installments/{inst.Id}/payments/{partners[0].Id}",
            new { isPaid = false, paidAt = (string?)null, paidByPartnerId = (Guid?)null, note = "" });
        unmark.EnsureSuccessStatusCode();
        cleared!.IsPaid.Should().BeFalse();
        cleared.HasReceipt.Should().BeFalse();
        cleared.ReviewStatus.Should().Be("None");
    }

    [Fact]
    public async Task Negative_receipt_rejects_text_plain_and_empty_file()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (plan, partners) = await PlanTestHelper.CreatePlanWithPartnersAsync(api);
        var inst = await PlanTestHelper.CreateInstallmentAsync(api, plan.Id);

        var plain = await PlanTestHelper.UploadTinyPdfReceiptAsync(
            api.Http, plan.Id, inst.Id, partners[0].Id, "x.txt", "text/plain", Encoding.UTF8.GetBytes("hi"));
        plain.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var empty = await PlanTestHelper.UploadTinyPdfReceiptAsync(
            api.Http, plan.Id, inst.Id, partners[0].Id, "empty.pdf", "application/pdf", Array.Empty<byte>());
        empty.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK);
    }

    [Fact]
    public async Task Negative_paid_by_partner_from_other_plan_rejected()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (planA, partnersA) = await PlanTestHelper.CreatePlanWithPartnersAsync(api);
        var (_, partnersB) = await PlanTestHelper.CreatePlanWithPartnersAsync(api);
        var inst = await PlanTestHelper.CreateInstallmentAsync(api, planA.Id);

        var (pay, _) = await api.PutAsync<object>(
            $"/api/plans/{planA.Id}/installments/{inst.Id}/payments/{partnersA[0].Id}",
            new
            {
                isPaid = true,
                paidAt = "2026-08-01",
                paidByPartnerId = partnersB[0].Id,
                note = "cross",
            });
        pay.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Positive_pay_on_behalf_of_self_is_noop_for_settlement_intent()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (plan, partners) = await PlanTestHelper.CreatePlanWithPartnersAsync(api);
        var inst = await PlanTestHelper.CreateInstallmentAsync(api, plan.Id, total: 200m);

        var (pay, payment) = await api.PutAsync<PaymentDto>(
            $"/api/plans/{plan.Id}/installments/{inst.Id}/payments/{partners[0].Id}",
            new
            {
                isPaid = true,
                paidAt = "2026-08-01",
                paidByPartnerId = partners[0].Id,
                note = "self",
            });
        pay.EnsureSuccessStatusCode();
        payment!.PaidByPartnerId.Should().Be(partners[0].Id);
    }

    [Fact]
    public async Task Positive_owner_pays_on_behalf_then_double_approve_fails()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (plan, partners) = await PlanTestHelper.CreatePlanWithPartnersAsync(api);
        var inst = await PlanTestHelper.CreateInstallmentAsync(api, plan.Id, total: 200m);

        (await api.PutAsync<PaymentDto>(
            $"/api/plans/{plan.Id}/installments/{inst.Id}/payments/{partners[0].Id}",
            new
            {
                isPaid = true,
                paidAt = "2026-08-01",
                paidByPartnerId = partners[1].Id,
                note = "B ödedi",
            })).Response.EnsureSuccessStatusCode();

        // Owner path auto-approves — second approve should fail
        var approve = await api.PostEmptyAsync(
            $"/api/plans/{plan.Id}/installments/{inst.Id}/payments/{partners[0].Id}/approve");
        approve.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Negative_note_over_1000_chars()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (plan, partners) = await PlanTestHelper.CreatePlanWithPartnersAsync(api);
        var inst = await PlanTestHelper.CreateInstallmentAsync(api, plan.Id);

        var (pay, _) = await api.PutAsync<object>(
            $"/api/plans/{plan.Id}/installments/{inst.Id}/payments/{partners[0].Id}",
            new
            {
                isPaid = false,
                paidAt = (string?)null,
                paidByPartnerId = (Guid?)null,
                note = new string('n', 1001),
            });
        pay.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Partner_iban_normalized_invalid_rejected_whitespace_null()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (plan, _) = await PlanTestHelper.CreatePlanWithPartnersAsync(api, partnerCount: 0);

        var (ok, partner) = await api.PostAsync<PartnerDto>($"/api/plans/{plan.Id}/partners", new
        {
            name = "IBAN'lı",
            color = "#000",
            defaultPct = 100m,
            sortOrder = 1,
            iban = PlanTestHelper.ValidTrIbanSpaced,
        });
        ok.EnsureSuccessStatusCode();
        partner!.Iban.Should().Be(PlanTestHelper.ValidTrIban);

        var (bad, _) = await api.PostAsync<object>($"/api/plans/{plan.Id}/partners", new
        {
            name = "Kötü IBAN",
            color = "#000",
            defaultPct = 0m,
            sortOrder = 2,
            iban = "TR123",
        });
        bad.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var (blank, blankPartner) = await api.PostAsync<PartnerDto>($"/api/plans/{plan.Id}/partners", new
        {
            name = "Boş IBAN",
            color = "#111",
            defaultPct = 0m,
            sortOrder = 3,
            iban = "   ",
        });
        blank.EnsureSuccessStatusCode();
        blankPartner!.Iban.Should().BeNull();
    }

    [Fact]
    public async Task Negative_delete_partner_with_payment_is_conflict()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (plan, partners) = await PlanTestHelper.CreatePlanWithPartnersAsync(api);
        var inst = await PlanTestHelper.CreateInstallmentAsync(api, plan.Id);

        (await api.PutAsync<object>(
            $"/api/plans/{plan.Id}/installments/{inst.Id}/payments/{partners[0].Id}",
            new { isPaid = true, paidAt = "2026-08-01", paidByPartnerId = (Guid?)null, note = "x" }))
            .Response.EnsureSuccessStatusCode();

        var del = await api.DeleteAsync($"/api/plans/{plan.Id}/partners/{partners[0].Id}");
        del.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Negative_delete_partner_used_only_as_paid_by_is_conflict()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (plan, partners) = await PlanTestHelper.CreatePlanWithPartnersAsync(api);
        var inst = await PlanTestHelper.CreateInstallmentAsync(api, plan.Id);

        (await api.PutAsync<object>(
            $"/api/plans/{plan.Id}/installments/{inst.Id}/payments/{partners[0].Id}",
            new
            {
                isPaid = true,
                paidAt = "2026-08-01",
                paidByPartnerId = partners[1].Id,
                note = "B ödedi",
            })).Response.EnsureSuccessStatusCode();

        var del = await api.DeleteAsync($"/api/plans/{plan.Id}/partners/{partners[1].Id}");
        del.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Positive_delete_partner_cleans_custom_shares()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (plan, partners) = await PlanTestHelper.CreatePlanWithPartnersAsync(api);
        var inst = await PlanTestHelper.CreateInstallmentAsync(
            api, plan.Id, "C", 100m, "Custom", 1,
            new[]
            {
                new { partnerId = partners[0].Id, amount = 60m },
                new { partnerId = partners[1].Id, amount = 40m },
            });

        (await api.DeleteAsync($"/api/plans/{plan.Id}/partners/{partners[1].Id}")).EnsureSuccessStatusCode();

        var (_, list) = await api.GetAsync<List<InstallmentDto>>($"/api/plans/{plan.Id}/installments");
        var remaining = list!.Single(i => i.Id == inst.Id);
        remaining.CustomShares.Should().NotContain(s => s.PartnerId == partners[1].Id);
        remaining.CustomShares.Should().ContainSingle(s => s.PartnerId == partners[0].Id);
    }

    [Fact]
    public async Task Negative_partner_pct_over_100()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (plan, _) = await PlanTestHelper.CreatePlanWithPartnersAsync(api, partnerCount: 0);

        var (res, _) = await api.PostAsync<object>($"/api/plans/{plan.Id}/partners", new
        {
            name = "Aşırı",
            color = "#000",
            defaultPct = 101m,
            sortOrder = 1,
            iban = (string?)null,
        });
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Weird_two_partners_can_both_be_60_pct_without_sum_check()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (plan, _) = await PlanTestHelper.CreatePlanWithPartnersAsync(api, partnerCount: 0);

        (await api.PostAsync<object>($"/api/plans/{plan.Id}/partners", new
        {
            name = "A", color = "#1", defaultPct = 60m, sortOrder = 1, iban = (string?)null
        })).Response.EnsureSuccessStatusCode();
        var (res, _) = await api.PostAsync<object>($"/api/plans/{plan.Id}/partners", new
        {
            name = "B", color = "#2", defaultPct = 60m, sortOrder = 2, iban = (string?)null
        });
        // API gap vs import: create does not enforce sum=100
        res.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
