using System.Net;
using PayDefteri.Api.Tests.Infrastructure;

namespace PayDefteri.Api.Tests;

[Collection("Api")]
public sealed class AccountDeletionPositiveNegativeTests
{
    private readonly TestClient _api;

    public AccountDeletionPositiveNegativeTests(ApiFixture fixture) =>
        _api = new TestClient(fixture.Factory.CreateClient());

    [Fact]
    public async Task Positive_owner_can_delete_the_account_and_the_credentials_stop_working()
    {
        var email = $"delete_{Guid.NewGuid():N}@example.com";
        await _api.RegisterAndLoginAsync(email);
        await PlanTestHelper.CreatePlanWithPartnersAsync(_api, "Silinecek plan");

        var deleted = await _api.DeleteAsync("/api/auth/account", new { currentPassword = "Secret123!" });
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);

        _api.ClearToken();
        var (login, _) = await _api.PostAsync<object>("/api/auth/login", new
        {
            email,
            password = "Secret123!",
        });
        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Positive_deleting_the_account_removes_the_plans_it_owned()
    {
        var email = $"delete_plans_{Guid.NewGuid():N}@example.com";
        await _api.RegisterAndLoginAsync(email);
        var (plan, _) = await PlanTestHelper.CreatePlanWithPartnersAsync(_api, "Kalmaması gereken plan");

        (await _api.DeleteAsync("/api/auth/account", new { currentPassword = "Secret123!" }))
            .EnsureSuccessStatusCode();

        // A fresh account must not be able to reach the deleted owner's plan.
        _api.ClearToken();
        await _api.RegisterAndLoginAsync();
        var (response, _) = await _api.GetAsync<object>($"/api/plans/{plan.Id}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Negative_wrong_password_is_rejected_and_the_account_survives()
    {
        var email = $"delete_wrong_{Guid.NewGuid():N}@example.com";
        await _api.RegisterAndLoginAsync(email);

        var refused = await _api.DeleteAsync("/api/auth/account", new { currentPassword = "WrongSecret123!" });
        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        _api.ClearToken();
        var (login, _) = await _api.PostAsync<TestClient.LoginDto>("/api/auth/login", new
        {
            email,
            password = "Secret123!",
        });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Negative_anonymous_caller_cannot_delete_an_account()
    {
        _api.ClearToken();

        var refused = await _api.DeleteAsync("/api/auth/account", new { currentPassword = "Secret123!" });
        refused.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
