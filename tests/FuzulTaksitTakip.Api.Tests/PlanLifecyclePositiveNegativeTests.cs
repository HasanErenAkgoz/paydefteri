using System.Net;
using System.Net.Http.Json;
using FuzulTaksitTakip.Api.Tests.Infrastructure;

namespace FuzulTaksitTakip.Api.Tests;

[Collection("Api")]
public sealed class PlanLifecyclePositiveNegativeTests
{
    private readonly ApiFixture _fixture;

    public PlanLifecyclePositiveNegativeTests(ApiFixture fixture) => _fixture = fixture;

    private TestClient NewClient() => new(_fixture.Factory.CreateClient());

    [Fact]
    public async Task Positive_Archive_hides_from_active_list_and_restore_brings_back()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();

        var plan = await CreatePlanAsync(api, "Arşivlenecek");
        var archive = await api.PostEmptyAsync($"/api/plans/{plan.Id}/archive");
        archive.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var (_, active) = await api.GetAsync<List<PlanDto>>("/api/plans");
        active!.Should().NotContain(p => p.Id == plan.Id);

        var (_, archived) = await api.GetAsync<List<PlanDto>>("/api/plans?includeArchived=true");
        archived!.Should().Contain(p => p.Id == plan.Id && p.IsArchived);

        var restore = await api.PostEmptyAsync($"/api/plans/{plan.Id}/restore");
        restore.StatusCode.Should().Be(HttpStatusCode.OK);

        var (_, activeAgain) = await api.GetAsync<List<PlanDto>>("/api/plans");
        activeAgain!.Should().Contain(p => p.Id == plan.Id);
    }

    [Fact]
    public async Task Positive_Hard_delete_removes_plan_permanently()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();

        var plan = await CreatePlanAsync(api, "Silinecek");
        (await api.PostEmptyAsync($"/api/plans/{plan.Id}/seed/empty")).EnsureSuccessStatusCode();

        var delete = await api.DeleteAsync($"/api/plans/{plan.Id}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var (_, active) = await api.GetAsync<List<PlanDto>>("/api/plans");
        active!.Should().NotContain(p => p.Id == plan.Id);

        var (_, archived) = await api.GetAsync<List<PlanDto>>("/api/plans?includeArchived=true");
        archived!.Should().NotContain(p => p.Id == plan.Id);

        var (get, _) = await api.GetAsync<object>($"/api/plans/{plan.Id}");
        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Positive_Copy_creates_new_plan_for_owner()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();

        var plan = await CreatePlanAsync(api, "Kaynak");
        (await api.PostEmptyAsync($"/api/plans/{plan.Id}/seed/empty")).EnsureSuccessStatusCode();

        var copyResponse = await api.PostEmptyAsync($"/api/plans/{plan.Id}/copy");
        copyResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var copied = await copyResponse.Content.ReadFromJsonAsync<PlanDto>();
        copied!.Id.Should().NotBe(plan.Id);
        copied.Title.Should().Contain("(kopya)");
    }

    [Fact]
    public async Task Positive_Seed_fuzul_grand_total_matches_golden_case()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();

        var plan = await CreatePlanAsync(api, "Fuzul");
        (await api.PostEmptyAsync($"/api/plans/{plan.Id}/seed/fuzul")).EnsureSuccessStatusCode();

        var (_, installments) = await api.GetAsync<List<InstallmentDto>>($"/api/plans/{plan.Id}/installments");
        installments.Should().NotBeNull();
        installments!.Should().HaveCount(20);
        installments!.Sum(i => i.TotalAmount).Should().Be(1_070_000m);
    }

    [Fact]
    public async Task Negative_Other_user_cannot_read_or_delete_foreign_plan()
    {
        var owner = NewClient();
        await owner.RegisterAndLoginAsync();
        var plan = await CreatePlanAsync(owner, "Gizli Plan");

        var stranger = NewClient();
        await stranger.RegisterAndLoginAsync();

        var (get, _) = await stranger.GetAsync<object>($"/api/plans/{plan.Id}");
        get.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);

        var delete = await stranger.DeleteAsync($"/api/plans/{plan.Id}");
        delete.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);

        var archive = await stranger.PostEmptyAsync($"/api/plans/{plan.Id}/archive");
        archive.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Negative_Restore_active_plan_is_not_found()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var plan = await CreatePlanAsync(api, "Aktif");

        var restore = await api.PostEmptyAsync($"/api/plans/{plan.Id}/restore");
        restore.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Negative_Archive_already_archived_is_not_found()
    {
        var api = NewClient();
        await api.RegisterAndLoginAsync();
        var plan = await CreatePlanAsync(api, "Çift arşiv");

        (await api.PostEmptyAsync($"/api/plans/{plan.Id}/archive")).EnsureSuccessStatusCode();
        var second = await api.PostEmptyAsync($"/api/plans/{plan.Id}/archive");
        second.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<PlanDto> CreatePlanAsync(TestClient api, string title)
    {
        var (response, plan) = await api.PostAsync<PlanDto>("/api/plans", new
        {
            title,
            description = "test",
        });
        response.EnsureSuccessStatusCode();
        return plan!;
    }
}
