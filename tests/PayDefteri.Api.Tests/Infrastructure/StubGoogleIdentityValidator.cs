using PayDefteri.Application.Common.Interfaces;

namespace PayDefteri.Api.Tests.Infrastructure;

/// <summary>
/// Stands in for Google so the sign-in flow can be exercised without a real ID
/// token. Only tokens handed to <see cref="Register"/> validate; everything else
/// is treated the same way a forged or expired token would be.
/// </summary>
public sealed class StubGoogleIdentityValidator : IGoogleIdentityValidator
{
    private readonly Dictionary<string, GoogleIdentity> _tokens = new(StringComparer.Ordinal);

    public bool IsConfigured { get; set; } = true;

    public string Register(string subject, string email, bool emailVerified = true, string? name = "Google Kullanıcısı")
    {
        var token = $"stub-token-{Guid.NewGuid():N}";
        _tokens[token] = new GoogleIdentity(subject, email, emailVerified, name);
        return token;
    }

    public Task<GoogleIdentity?> ValidateAsync(string idToken, CancellationToken cancellationToken = default) =>
        Task.FromResult(_tokens.TryGetValue(idToken, out var identity) ? identity : null);
}
