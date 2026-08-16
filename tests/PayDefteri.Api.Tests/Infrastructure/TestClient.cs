using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PayDefteri.Api.Tests.Infrastructure;

public sealed class TestClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient _http;

    public TestClient(HttpClient http) => _http = http;

    public HttpClient Http => _http;

    public void UseToken(string token) =>
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    public void ClearToken() => _http.DefaultRequestHeaders.Authorization = null;

    public async Task<(HttpResponseMessage Response, T? Body)> PostAsync<T>(string url, object body)
    {
        var response = await _http.PostAsJsonAsync(url, body);
        var parsed = await ReadAsync<T>(response);
        return (response, parsed);
    }

    public async Task<(HttpResponseMessage Response, T? Body)> PutAsync<T>(string url, object body)
    {
        var response = await _http.PutAsJsonAsync(url, body);
        var parsed = await ReadAsync<T>(response);
        return (response, parsed);
    }

    public async Task<(HttpResponseMessage Response, T? Body)> GetAsync<T>(string url)
    {
        var response = await _http.GetAsync(url);
        var parsed = await ReadAsync<T>(response);
        return (response, parsed);
    }

    public Task<HttpResponseMessage> DeleteAsync(string url) => _http.DeleteAsync(url);

    public Task<HttpResponseMessage> PostEmptyAsync(string url) => _http.PostAsync(url, null);

    public async Task<string> RegisterAndLoginAsync(string? email = null, string password = "Secret123!", string displayName = "Test User")
    {
        email ??= $"user_{Guid.NewGuid():N}@example.com";
        var register = await _http.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            displayName,
        });
        register.EnsureSuccessStatusCode();

        var loginResponse = await _http.PostAsJsonAsync("/api/auth/login", new { email, password });
        loginResponse.EnsureSuccessStatusCode();
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginDto>(JsonOptions)
            ?? throw new InvalidOperationException("Login response body was empty.");
        if (string.IsNullOrWhiteSpace(login.AccessToken))
        {
            throw new InvalidOperationException("Login did not return an access token.");
        }

        UseToken(login.AccessToken);
        return login.AccessToken;
    }

    private static async Task<T?> ReadAsync<T>(HttpResponseMessage response)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            return default;
        }

        var text = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(text))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(text, JsonOptions);
    }

    public sealed record LoginDto(string AccessToken, DateTime ExpiresAt);
}
