using System.Net;
using System.Net.Http.Json;
using FuzulTaksitTakip.Application.Auth;
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
            password = "Secret123!",
            displayName = "Pozitif Kullanıcı",
        });
        reg.StatusCode.Should().Be(HttpStatusCode.Created);

        var (login, body) = await _api.PostAsync<TestClient.LoginDto>("/api/auth/login", new
        {
            email,
            password = "Secret123!",
        });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Positive_Register_then_authenticated_xsrf_token_allows_cookie_write_request()
    {
        var email = $"cookie_{Guid.NewGuid():N}@example.com";
        var (register, _) = await _api.PostAsync<object>("/api/auth/register", new
        {
            email,
            password = "Secret123!",
            displayName = "Cookie Kullanıcısı",
        });

        register.StatusCode.Should().Be(HttpStatusCode.Created);
        register.Headers.GetValues("Set-Cookie").Should().Contain(value =>
            value.StartsWith("paydefteri_session=", StringComparison.Ordinal)
            && value.Contains("httponly", StringComparison.OrdinalIgnoreCase));

        var (me, profile) = await _api.GetAsync<UserProfileDto>("/api/auth/me");
        me.StatusCode.Should().Be(HttpStatusCode.OK);
        profile!.Email.Should().Be(email);

        var xsrf = await _api.Http.GetAsync("/api/auth/xsrf");
        xsrf.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var xsrfCookie = xsrf.Headers.GetValues("Set-Cookie").Single(value =>
            value.StartsWith("paydefteri_xsrf=", StringComparison.Ordinal));
        var xsrfToken = xsrfCookie["paydefteri_xsrf=".Length..xsrfCookie.IndexOf(';')];

        using var createPlan = new HttpRequestMessage(HttpMethod.Post, "/api/plans")
        {
            Content = JsonContent.Create(new
            {
                title = "Cookie Planı",
                description = "XSRF doğrulama testi",
                planType = "Installment",
            }),
        };
        createPlan.Headers.Add("X-XSRF-TOKEN", xsrfToken);

        var created = await _api.Http.SendAsync(createPlan);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Negative_Login_with_wrong_password_is_forbidden()
    {
        var email = $"neg_{Guid.NewGuid():N}@example.com";
        (await _api.PostAsync<object>("/api/auth/register", new
        {
            email,
            password = "Secret123!",
            displayName = "Negatif",
        })).Response.EnsureSuccessStatusCode();

        var (login, _) = await _api.PostAsync<object>("/api/auth/login", new
        {
            email,
            password = "WrongPassword!",
        });
        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Negative_Register_with_invalid_email_is_bad_request()
    {
        var (reg, _) = await _api.PostAsync<object>("/api/auth/register", new
        {
            email = "not-an-email",
            password = "Secret123!",
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
            password = "Secret123!",
            displayName = "One",
        })).Response.EnsureSuccessStatusCode();

        var (second, _) = await _api.PostAsync<object>("/api/auth/register", new
        {
            email,
            password = "Secret123!",
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
