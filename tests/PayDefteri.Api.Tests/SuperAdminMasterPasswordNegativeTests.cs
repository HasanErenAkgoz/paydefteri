using System.Net;
using Microsoft.Extensions.DependencyInjection;
using PayDefteri.Api.Tests.Infrastructure;
using PayDefteri.Infrastructure.Identity;

namespace PayDefteri.Api.Tests;

/// <summary>
/// Regression coverage for the P0 fixed in IdentityService: the SuperAdmin seed
/// password must never work as a master password against another account.
/// </summary>
[Collection("Api")]
public sealed class SuperAdminMasterPasswordNegativeTests
{
    private readonly ApiFixture _fixture;
    private readonly TestClient _api;

    public SuperAdminMasterPasswordNegativeTests(ApiFixture fixture)
    {
        _fixture = fixture;
        _api = new TestClient(fixture.Factory.CreateClient());
    }

    private async Task SeedSuperAdminAsync()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        await IdentityDataSeeder.SeedAsync(scope.ServiceProvider);
    }

    [Fact]
    public async Task Negative_SuperAdmin_seed_password_does_not_log_into_another_users_account()
    {
        await SeedSuperAdminAsync();

        var email = $"victim_{Guid.NewGuid():N}@example.com";
        (await _api.PostAsync<object>("/api/auth/register", new
        {
            email,
            password = "RealPassword123!",
            displayName = "Kurban",
        })).Response.EnsureSuccessStatusCode();

        var (login, _) = await _api.PostAsync<object>("/api/auth/login", new
        {
            email,
            password = ApiFactory.SuperAdminSeedPassword,
        });

        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Negative_SuperAdmin_seed_password_cannot_change_another_users_password()
    {
        await SeedSuperAdminAsync();

        var email = $"victim_{Guid.NewGuid():N}@example.com";
        await _api.RegisterAndLoginAsync(email, "RealPassword123!", "Kurban");

        var (change, _) = await _api.PostAsync<object>("/api/auth/change-password", new
        {
            currentPassword = ApiFactory.SuperAdminSeedPassword,
            newPassword = "Hijacked123!",
        });

        change.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var (login, _) = await _api.PostAsync<object>("/api/auth/login", new
        {
            email,
            password = "RealPassword123!",
        });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Positive_SuperAdmin_can_still_log_in_with_its_own_seeded_password()
    {
        await SeedSuperAdminAsync();

        var (login, body) = await _api.PostAsync<TestClient.LoginDto>("/api/auth/login", new
        {
            email = "superadmin@paydefteri.com",
            password = ApiFactory.SuperAdminSeedPassword,
        });

        login.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }
}
