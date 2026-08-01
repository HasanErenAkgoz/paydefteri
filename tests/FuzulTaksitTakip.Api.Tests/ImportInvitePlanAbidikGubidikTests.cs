using System.Net;
using System.Net.Http.Json;
using FuzulTaksitTakip.Api.Tests.Infrastructure;

namespace FuzulTaksitTakip.Api.Tests;

[Collection("Api")]
public sealed class ImportInvitePlanAbidikGubidikTests
{
    private readonly ApiFixture _fixture;
    public ImportInvitePlanAbidikGubidikTests(ApiFixture fixture) => _fixture = fixture;
    private TestClient NewClient() => new(_fixture.Factory.CreateClient());

    [Fact]
    public async Task Negative_import_pct_not_100_and_unknown_share_type()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (plan, _) = await PlanTestHelper.CreatePlanWithPartnersAsync(api, partnerCount: 0);

        var badPct = PlanTestHelper.MinimalExport(pctA: 70m, pctB: 20m);
        var (pctRes, _) = await api.PostAsync<object>($"/api/plans/{plan.Id}/import", badPct);
        pctRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var badType = new
        {
            title = "X",
            description = "",
            deliveryInstallmentId = (Guid?)null,
            partners = new[]
            {
                new { id = a, name = "A", color = "#1", defaultPct = 50m, sortOrder = 1 },
                new { id = b, name = "B", color = "#2", defaultPct = 50m, sortOrder = 2 },
            },
            installments = new[]
            {
                new
                {
                    id = Guid.NewGuid(),
                    name = "T",
                    dueDate = "2026-09-01",
                    totalAmount = 10m,
                    shareType = "telepati",
                    sortOrder = 1,
                    customShares = Array.Empty<object>(),
                    payments = Array.Empty<object>(),
                },
            },
        };
        var (typeRes, _) = await api.PostAsync<object>($"/api/plans/{plan.Id}/import", badType);
        typeRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Negative_import_zero_partners_and_unknown_custom_partner()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (plan, _) = await PlanTestHelper.CreatePlanWithPartnersAsync(api, partnerCount: 0);

        var (zero, _) = await api.PostAsync<object>($"/api/plans/{plan.Id}/import", new
        {
            title = "Boş",
            description = "",
            deliveryInstallmentId = (Guid?)null,
            partners = Array.Empty<object>(),
            installments = Array.Empty<object>(),
        });
        zero.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var ghost = Guid.NewGuid();
        var (custom, _) = await api.PostAsync<object>($"/api/plans/{plan.Id}/import", new
        {
            title = "Ghost share",
            description = "",
            deliveryInstallmentId = (Guid?)null,
            partners = new[]
            {
                new { id = a, name = "A", color = "#1", defaultPct = 50m, sortOrder = 1 },
                new { id = b, name = "B", color = "#2", defaultPct = 50m, sortOrder = 2 },
            },
            installments = new[]
            {
                new
                {
                    id = Guid.NewGuid(),
                    name = "T",
                    dueDate = "2026-09-01",
                    totalAmount = 100m,
                    shareType = "Custom",
                    sortOrder = 1,
                    customShares = new[]
                    {
                        new { partnerId = a, amount = 50m },
                        new { partnerId = ghost, amount = 50m },
                    },
                    payments = Array.Empty<object>(),
                },
            },
        });
        custom.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Positive_export_import_round_trip_preserves_amounts()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (plan, _) = await PlanTestHelper.CreatePlanWithPartnersAsync(api);
        await PlanTestHelper.CreateInstallmentAsync(api, plan.Id, "RT", 1234.56m);

        var (exp, export) = await api.GetAsync<PlanExportDto>($"/api/plans/{plan.Id}/export");
        exp.EnsureSuccessStatusCode();
        export!.Installments.Should().Contain(i => i.TotalAmount == 1234.56m);

        // Re-import into same plan (IDs are globally unique — cross-plan reuse 500s)
        var (imp, _) = await api.PostAsync<PlanDto>($"/api/plans/{plan.Id}/import", export);
        imp.EnsureSuccessStatusCode();

        var (_, list) = await api.GetAsync<List<InstallmentDto>>($"/api/plans/{plan.Id}/installments");
        list!.Should().Contain(i => i.TotalAmount == 1234.56m);
    }

    [Fact]
    public async Task Weird_import_same_ids_into_other_plan_hits_pk_collision_500()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (plan, _) = await PlanTestHelper.CreatePlanWithPartnersAsync(api);
        await PlanTestHelper.CreateInstallmentAsync(api, plan.Id, "RT", 10m);
        var (_, export) = await api.GetAsync<PlanExportDto>($"/api/plans/{plan.Id}/export");

        var (plan2, _) = await PlanTestHelper.CreatePlanWithPartnersAsync(api, partnerCount: 0);
        var (imp, _) = await api.PostAsync<object>($"/api/plans/{plan2.Id}/import", export!);
        // Documents current gap: installment/partner PKs are global, import reuses export ids
        imp.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Negative_stranger_cannot_export_or_import()
    {
        var owner = NewClient();
        await owner.RegisterAndLoginAsync();
        var (plan, _) = await PlanTestHelper.CreatePlanWithPartnersAsync(owner);

        var stranger = NewClient();
        await stranger.RegisterAndLoginAsync();

        var (exp, _) = await stranger.GetAsync<object>($"/api/plans/{plan.Id}/export");
        exp.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);

        var (imp, _) = await stranger.PostAsync<object>(
            $"/api/plans/{plan.Id}/import",
            PlanTestHelper.MinimalExport());
        imp.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Invite_wrong_email_forbidden_revoke_then_accept_conflict_preview_anonymous()
    {
        var owner = NewClient();
        await owner.RegisterAndLoginAsync();
        var (plan, partners) = await PlanTestHelper.CreatePlanWithPartnersAsync(owner);

        var inviteEmail = $"invite_{Guid.NewGuid():N}@example.com";
        var (invRes, invite) = await owner.PostAsync<InviteCreatedDto>(
            $"/api/plans/{plan.Id}/invites",
            new { email = inviteEmail, partnerId = partners[0].Id });
        invRes.EnsureSuccessStatusCode();
        invite!.Token.Should().NotBeNullOrWhiteSpace();

        var previewClient = _fixture.Factory.CreateClient();
        var previewHttp = new TestClient(previewClient);
        previewHttp.ClearToken();
        var (preview, previewBody) = await previewHttp.GetAsync<InvitePreviewDto>(
            $"/api/invites/{invite.Token}/preview");
        preview.StatusCode.Should().Be(HttpStatusCode.OK);
        previewBody!.IsAcceptable.Should().BeTrue();

        var wrongUser = NewClient();
        await wrongUser.RegisterAndLoginAsync($"wrong_{Guid.NewGuid():N}@example.com");
        var (acceptWrong, _) = await wrongUser.PostAsync<object>($"/api/invites/{invite.Token}/accept", new { });
        acceptWrong.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await owner.DeleteAsync($"/api/plans/{plan.Id}/invites/{invite.Id}")).EnsureSuccessStatusCode();

        var invitee = NewClient();
        await invitee.RegisterAndLoginAsync(inviteEmail);
        var (acceptRevoked, _) = await invitee.PostAsync<object>($"/api/invites/{invite.Token}/accept", new { });
        acceptRevoked.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Positive_invite_accept_with_matching_email_then_second_accept_conflicts()
    {
        var owner = NewClient();
        await owner.RegisterAndLoginAsync();
        var (plan, partners) = await PlanTestHelper.CreatePlanWithPartnersAsync(owner);
        var email = $"ok_{Guid.NewGuid():N}@example.com";

        var (invRes, invite) = await owner.PostAsync<InviteCreatedDto>(
            $"/api/plans/{plan.Id}/invites",
            new { email, partnerId = partners[0].Id });
        invRes.EnsureSuccessStatusCode();

        var invitee = NewClient();
        await invitee.RegisterAndLoginAsync(email);
        var (accept, joined) = await invitee.PostAsync<PlanDto>($"/api/invites/{invite!.Token}/accept", new { });
        accept.StatusCode.Should().Be(HttpStatusCode.OK);
        joined!.Id.Should().Be(plan.Id);

        var (again, _) = await invitee.PostAsync<object>($"/api/invites/{invite.Token}/accept", new { });
        again.StatusCode.Should().BeOneOf(HttpStatusCode.Conflict, HttpStatusCode.BadRequest, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Negative_invite_invalid_email_and_foreign_partner()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (planA, _) = await PlanTestHelper.CreatePlanWithPartnersAsync(api);
        var (planB, partnersB) = await PlanTestHelper.CreatePlanWithPartnersAsync(api);

        var (badEmail, _) = await api.PostAsync<object>(
            $"/api/plans/{planA.Id}/invites",
            new { email = "not-mail", partnerId = Guid.NewGuid() });
        badEmail.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var (foreign, _) = await api.PostAsync<object>(
            $"/api/plans/{planA.Id}/invites",
            new { email = $"x_{Guid.NewGuid():N}@example.com", partnerId = partnersB[0].Id });
        foreign.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Plan_settings_reject_require_receipt_with_iban_none_and_duplicate_reminder_days()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (plan, _) = await PlanTestHelper.CreatePlanWithPartnersAsync(api);

        var receiptNone = await PlanTestHelper.UpdatePlanRawAsync(api, plan.Id, new
        {
            title = plan.Title,
            description = "x",
            deliveryInstallmentId = (Guid?)null,
            requireReceipt = true,
            ibanMode = "None",
            settlementIban = (string?)null,
            remindersEnabled = false,
            reminderDaysBefore = Array.Empty<int>(),
            reminderDaysAfter = Array.Empty<int>(),
        });
        receiptNone.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var planIbanMissing = await PlanTestHelper.UpdatePlanRawAsync(api, plan.Id, new
        {
            title = plan.Title,
            description = "x",
            deliveryInstallmentId = (Guid?)null,
            requireReceipt = false,
            ibanMode = "Plan",
            settlementIban = (string?)null,
            remindersEnabled = false,
            reminderDaysBefore = Array.Empty<int>(),
            reminderDaysAfter = Array.Empty<int>(),
        });
        planIbanMissing.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var dupDays = await PlanTestHelper.UpdatePlanRawAsync(api, plan.Id, new
        {
            title = plan.Title,
            description = "x",
            deliveryInstallmentId = (Guid?)null,
            requireReceipt = false,
            ibanMode = "None",
            settlementIban = (string?)null,
            remindersEnabled = true,
            reminderDaysBefore = new[] { 1, 1 },
            reminderDaysAfter = Array.Empty<int>(),
        });
        dupDays.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var tooMany = await PlanTestHelper.UpdatePlanRawAsync(api, plan.Id, new
        {
            title = plan.Title,
            description = "x",
            deliveryInstallmentId = (Guid?)null,
            requireReceipt = false,
            ibanMode = "None",
            settlementIban = (string?)null,
            remindersEnabled = true,
            reminderDaysBefore = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 },
            reminderDaysAfter = Array.Empty<int>(),
        });
        tooMany.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var okIban = await PlanTestHelper.UpdatePlanRawAsync(api, plan.Id, new
        {
            title = plan.Title,
            description = "x",
            deliveryInstallmentId = (Guid?)null,
            requireReceipt = false,
            ibanMode = "Plan",
            settlementIban = PlanTestHelper.ValidTrIbanSpaced,
            remindersEnabled = false,
            reminderDaysBefore = Array.Empty<int>(),
            reminderDaysAfter = Array.Empty<int>(),
        });
        okIban.EnsureSuccessStatusCode();
        var updated = await okIban.Content.ReadFromJsonAsync<PlanDto>();
        // SettlementIban may not be on slim PlanDto — ignore if null in our DTO
        _ = updated;
    }

    [Fact]
    public async Task Weird_owner_can_still_mutate_archived_plan_via_ensure_owner_quirk()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (plan, _) = await PlanTestHelper.CreatePlanWithPartnersAsync(api);
        (await api.PostEmptyAsync($"/api/plans/{plan.Id}/archive")).EnsureSuccessStatusCode();

        // EnsureOwner ignores IsDeleted — documents current quirk
        var (create, _) = await api.PostAsync<object>($"/api/plans/{plan.Id}/installments", new
        {
            name = "Arşivden taksit",
            dueDate = "2026-09-01",
            totalAmount = 1m,
            shareType = "Equal",
            sortOrder = 1,
            customShares = (object?)null,
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        var stranger = NewClient();
        await stranger.RegisterAndLoginAsync();
        var (get, _) = await stranger.GetAsync<object>($"/api/plans/{plan.Id}");
        get.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Positive_settle_up_and_double_submit_are_idempotent_enough()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (plan, partners) = await PlanTestHelper.CreatePlanWithPartnersAsync(api);
        var inst = await PlanTestHelper.CreateInstallmentAsync(api, plan.Id, total: 100m);

        (await api.PutAsync<object>(
            $"/api/plans/{plan.Id}/installments/{inst.Id}/payments/{partners[0].Id}",
            new
            {
                isPaid = true,
                paidAt = "2026-08-01",
                paidByPartnerId = partners[1].Id,
                note = "B ödedi",
            })).Response.EnsureSuccessStatusCode();

        (await api.PostEmptyAsync($"/api/plans/{plan.Id}/settle-up")).EnsureSuccessStatusCode();
        (await api.PostEmptyAsync($"/api/plans/{plan.Id}/settle-up")).EnsureSuccessStatusCode();

        var (_, list) = await api.GetAsync<List<InstallmentDto>>($"/api/plans/{plan.Id}/installments");
        var payment = list!.Single().Payments.Single(p => p.PartnerId == partners[0].Id);
        payment.PaidByPartnerId.Should().Be(partners[0].Id);
    }

    [Fact]
    public async Task Negative_create_plan_empty_title_and_login_empty_password()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();

        var (emptyTitle, _) = await api.PostAsync<object>("/api/plans", new { title = "", description = "x" });
        emptyTitle.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var (login, _) = await api.PostAsync<object>("/api/auth/login", new { email = "a@b.com", password = "" });
        login.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Positive_seed_unknown_template_is_not_found()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var (plan, _) = await PlanTestHelper.CreatePlanWithPartnersAsync(api, partnerCount: 0);

        var seed = await api.PostEmptyAsync($"/api/plans/{plan.Id}/seed/yok-boyle-bir-sey");
        seed.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

public sealed record InviteCreatedDto(
    Guid Id,
    string Email,
    string Token,
    string Status);
