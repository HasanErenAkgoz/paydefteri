using System.Net;
using FuzulTaksitTakip.Api.Tests.Infrastructure;

namespace FuzulTaksitTakip.Api.Tests;

[Collection("Api")]
public sealed class AuthPositiveNegativeTests
{
    private readonly TestClient _api;

    public AuthPositiveNegativeTests(ApiFixture fixture) =>
        _api = new TestClient(fixture.Factory.CreateClient());

    [Fact]
    public async Task Positive_Register_then_login_returns_jwt()
    {
        var email = $"pos_{Guid.NewGuid():N}@example.com";
        var (reg, _) = await _api.PostAsync<object>("/api/auth/register", new
        {
            email,
            password = "Secret1!",
            displayName = "Pozitif Kullanıcı",
        });
        reg.StatusCode.Should().Be(HttpStatusCode.Created);

        var (login, body) = await _api.PostAsync<TestClient.LoginDto>("/api/auth/login", new
        {
            email,
            password = "Secret1!",
        });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Negative_Login_with_wrong_password_is_forbidden()
    {
        var email = $"neg_{Guid.NewGuid():N}@example.com";
        (await _api.PostAsync<object>("/api/auth/register", new
        {
            email,
            password = "Secret1!",
            displayName = "Negatif",
        })).Response.EnsureSuccessStatusCode();

        var (login, _) = await _api.PostAsync<object>("/api/auth/login", new
        {
            email,
            password = "WrongPassword!",
        });
        login.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Negative_Register_with_invalid_email_is_bad_request()
    {
        var (reg, _) = await _api.PostAsync<object>("/api/auth/register", new
        {
            email = "not-an-email",
            password = "Secret1!",
            displayName = "X",
        });
        reg.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Negative_Register_duplicate_email_is_bad_request()
    {
        var email = $"dup_{Guid.NewGuid():N}@example.com";
        (await _api.PostAsync<object>("/api/auth/register", new
        {
            email,
            password = "Secret1!",
            displayName = "One",
        })).Response.EnsureSuccessStatusCode();

        var (second, _) = await _api.PostAsync<object>("/api/auth/register", new
        {
            email,
            password = "Secret1!",
            displayName = "Two",
        });
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Negative_Plans_without_token_is_unauthorized()
    {
        _api.ClearToken();
        var (response, _) = await _api.GetAsync<object>("/api/plans");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
