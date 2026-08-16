using System.Net;
using System.Net.Http.Json;
using FuzulTaksitTakip.Api.Tests.Infrastructure;

namespace FuzulTaksitTakip.Api.Tests;

[Collection("Api")]
public sealed class MobileAuthPositiveNegativeTests
{
    private readonly TestClient _api;

    public MobileAuthPositiveNegativeTests(ApiFixture fixture) =>
        _api = new TestClient(fixture.Factory.CreateClient());

    [Fact]
    public async Task Positive_Mobile_register_returns_rotatable_session_and_profile()
    {
        var email = $"mobile_{Guid.NewGuid():N}@example.com";
        var (register, initial) = await _api.PostAsync<MobileAuthDto>(
            "/api/mobile/v1/auth/register",
            new
            {
                email,
                password = "Secret123!",
                displayName = "Mobil Kullanıcı",
                device = Device(),
            });

        register.StatusCode.Should().Be(HttpStatusCode.Created);
        initial!.AccessToken.Should().NotBeNullOrWhiteSpace();
        initial.RefreshToken.Should().NotBeNullOrWhiteSpace();
        initial.User.Email.Should().Be(email);

        _api.UseToken(initial.AccessToken);
        _api.Http.DefaultRequestHeaders.Add("X-Mobile-Session-Id", initial.SessionId.ToString());
        var (sessionsResponse, sessions) = await _api.GetAsync<List<MobileSessionDto>>(
            "/api/mobile/v1/auth/sessions");
        sessionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        sessions.Should().ContainSingle(x => x.Id == initial.SessionId && x.IsCurrent);

        var (refresh, rotated) = await _api.PostAsync<MobileAuthDto>(
            "/api/mobile/v1/auth/refresh",
            new { refreshToken = initial.RefreshToken });
        refresh.StatusCode.Should().Be(HttpStatusCode.OK);
        rotated!.RefreshToken.Should().NotBe(initial.RefreshToken);
        rotated.SessionId.Should().NotBe(initial.SessionId);
    }

    [Fact]
    public async Task Negative_Reused_refresh_token_revokes_the_rotated_family()
    {
        var initial = await RegisterMobileAsync();
        var (_, rotated) = await _api.PostAsync<MobileAuthDto>(
            "/api/mobile/v1/auth/refresh",
            new { refreshToken = initial.RefreshToken });

        var (replay, _) = await _api.PostAsync<object>(
            "/api/mobile/v1/auth/refresh",
            new { refreshToken = initial.RefreshToken });
        replay.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var (familyRefresh, _) = await _api.PostAsync<object>(
            "/api/mobile/v1/auth/refresh",
            new { refreshToken = rotated!.RefreshToken });
        familyRefresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Positive_Mobile_logout_is_idempotent_and_blocks_refresh()
    {
        var initial = await RegisterMobileAsync();
        var (logout, _) = await _api.PostAsync<object>(
            "/api/mobile/v1/auth/logout",
            new { refreshToken = initial.RefreshToken });
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var (secondLogout, _) = await _api.PostAsync<object>(
            "/api/mobile/v1/auth/logout",
            new { refreshToken = initial.RefreshToken });
        secondLogout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var (refresh, _) = await _api.PostAsync<object>(
            "/api/mobile/v1/auth/refresh",
            new { refreshToken = initial.RefreshToken });
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Negative_Concurrent_refresh_allows_only_one_rotation_and_revokes_family()
    {
        var initial = await RegisterMobileAsync();
        var body = new { refreshToken = initial.RefreshToken };

        var responses = await Task.WhenAll(
            _api.Http.PostAsJsonAsync("/api/mobile/v1/auth/refresh", body),
            _api.Http.PostAsJsonAsync("/api/mobile/v1/auth/refresh", body));

        responses.Count(x => x.StatusCode == HttpStatusCode.OK).Should().Be(1);
        responses.Count(x => x.StatusCode == HttpStatusCode.Unauthorized).Should().Be(1);

        var winner = responses.Single(x => x.StatusCode == HttpStatusCode.OK);
        var rotated = await winner.Content.ReadFromJsonAsync<MobileAuthDto>();
        var (afterRace, _) = await _api.PostAsync<object>(
            "/api/mobile/v1/auth/refresh",
            new { refreshToken = rotated!.RefreshToken });
        afterRace.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<MobileAuthDto> RegisterMobileAsync()
    {
        var (_, result) = await _api.PostAsync<MobileAuthDto>(
            "/api/mobile/v1/auth/register",
            new
            {
                email = $"mobile_{Guid.NewGuid():N}@example.com",
                password = "Secret123!",
                displayName = "Mobil Test",
                device = Device(),
            });
        return result!;
    }

    private static object Device() => new
    {
        deviceName = "Pixel Test",
        platform = "android",
        appVersion = "1.0.0",
    };

    private sealed record MobileAuthDto(
        string AccessToken,
        DateTime AccessTokenExpiresAt,
        string RefreshToken,
        DateTime RefreshTokenExpiresAt,
        Guid SessionId,
        MobileUserDto User);

    private sealed record MobileUserDto(string UserId, string Email, string DisplayName);

    private sealed record MobileSessionDto(
        Guid Id,
        string DeviceName,
        string Platform,
        string AppVersion,
        DateTime CreatedAtUtc,
        DateTime? LastUsedAtUtc,
        DateTime ExpiresAtUtc,
        bool IsCurrent);
}
