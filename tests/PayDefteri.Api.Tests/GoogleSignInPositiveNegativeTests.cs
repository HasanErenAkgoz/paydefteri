using System.Net;
using Microsoft.Extensions.DependencyInjection;
using PayDefteri.Api.Tests.Infrastructure;

namespace PayDefteri.Api.Tests;

[Collection("Api")]
public sealed class GoogleSignInPositiveNegativeTests
{
    private readonly TestClient _api;
    private readonly StubGoogleIdentityValidator _google;

    public GoogleSignInPositiveNegativeTests(ApiFixture fixture)
    {
        _api = new TestClient(fixture.Factory.CreateClient());
        _google = fixture.Factory.Services.GetRequiredService<StubGoogleIdentityValidator>();
    }

    [Fact]
    public async Task Positive_first_google_sign_in_creates_an_account_that_needs_no_email_verification()
    {
        var email = $"google_new_{Guid.NewGuid():N}@example.com";
        var token = _google.Register($"sub_{Guid.NewGuid():N}", email, name: "Yeni Kullanıcı");

        var (response, login) = await _api.PostAsync<TestClient.LoginDto>("/api/auth/google", new { idToken = token });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        login!.AccessToken.Should().NotBeNullOrWhiteSpace();

        _api.UseToken(login.AccessToken);
        var (_, profile) = await _api.GetAsync<ProfileDto>("/api/auth/me");
        profile!.Email.Should().Be(email);
        profile.DisplayName.Should().Be("Yeni Kullanıcı");
        profile.EmailConfirmed.Should().BeTrue();
        // Nothing set a password, so the profile screen must not ask for one.
        profile.HasPassword.Should().BeFalse();
    }

    [Fact]
    public async Task Positive_second_google_sign_in_returns_the_same_account()
    {
        var email = $"google_repeat_{Guid.NewGuid():N}@example.com";
        var subject = $"sub_{Guid.NewGuid():N}";

        var (first, firstLogin) = await _api.PostAsync<TestClient.LoginDto>(
            "/api/auth/google",
            new { idToken = _google.Register(subject, email) });
        first.EnsureSuccessStatusCode();
        _api.UseToken(firstLogin!.AccessToken);
        var (_, firstProfile) = await _api.GetAsync<ProfileDto>("/api/auth/me");

        _api.ClearToken();
        var (second, secondLogin) = await _api.PostAsync<TestClient.LoginDto>(
            "/api/auth/google",
            new { idToken = _google.Register(subject, email) });
        second.EnsureSuccessStatusCode();
        _api.UseToken(secondLogin!.AccessToken);
        var (_, secondProfile) = await _api.GetAsync<ProfileDto>("/api/auth/me");

        secondProfile!.UserId.Should().Be(firstProfile!.UserId);
    }

    [Fact]
    public async Task Positive_google_sign_in_links_the_existing_password_account_with_the_same_address()
    {
        var email = $"google_link_{Guid.NewGuid():N}@example.com";
        await _api.RegisterAndLoginAsync(email, displayName: "Şifreli Hesap");
        var (_, registered) = await _api.GetAsync<ProfileDto>("/api/auth/me");

        _api.ClearToken();
        var (response, login) = await _api.PostAsync<TestClient.LoginDto>(
            "/api/auth/google",
            new { idToken = _google.Register($"sub_{Guid.NewGuid():N}", email) });
        response.EnsureSuccessStatusCode();

        _api.UseToken(login!.AccessToken);
        var (_, linked) = await _api.GetAsync<ProfileDto>("/api/auth/me");

        // Same account, not a duplicate — and the password still works.
        linked!.UserId.Should().Be(registered!.UserId);
        linked.HasPassword.Should().BeTrue();

        _api.ClearToken();
        var (passwordLogin, _) = await _api.PostAsync<TestClient.LoginDto>(
            "/api/auth/login",
            new { email, password = "Secret123!" });
        passwordLogin.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Positive_a_google_only_account_can_be_deleted_without_a_password()
    {
        var email = $"google_delete_{Guid.NewGuid():N}@example.com";
        var (response, login) = await _api.PostAsync<TestClient.LoginDto>(
            "/api/auth/google",
            new { idToken = _google.Register($"sub_{Guid.NewGuid():N}", email) });
        response.EnsureSuccessStatusCode();
        _api.UseToken(login!.AccessToken);

        var deleted = await _api.DeleteAsync("/api/auth/account", new { currentPassword = (string?)null });

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Negative_an_unverified_google_address_cannot_claim_an_account()
    {
        var email = $"google_unverified_{Guid.NewGuid():N}@example.com";
        await _api.RegisterAndLoginAsync(email);
        _api.ClearToken();

        var (response, _) = await _api.PostAsync<object>(
            "/api/auth/google",
            new { idToken = _google.Register($"sub_{Guid.NewGuid():N}", email, emailVerified: false) });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Negative_a_token_google_never_issued_is_rejected()
    {
        var (response, _) = await _api.PostAsync<object>(
            "/api/auth/google",
            new { idToken = "not-a-real-google-token" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Negative_an_empty_token_is_rejected_before_any_account_lookup()
    {
        var (response, _) = await _api.PostAsync<object>("/api/auth/google", new { idToken = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record ProfileDto(
        string UserId,
        string Email,
        string DisplayName,
        bool EmailConfirmed,
        bool HasPassword);
}
