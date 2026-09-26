using Google.Apis.Auth;
using PayDefteri.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PayDefteri.Infrastructure.Auth;

public sealed class GoogleAuthOptions
{
    public const string SectionName = "Authentication:Google";

    /// <summary>Web (OAuth "Web application") client id — the one the browser button uses.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Extra client ids accepted as an audience. Native sign-in mints tokens for
    /// the iOS/Android client ids, not the web one, so both have to be allowed.
    /// </summary>
    public string[] AdditionalClientIds { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> Audiences() => new[] { ClientId }
        .Concat(AdditionalClientIds)
        .Where(id => !string.IsNullOrWhiteSpace(id))
        .Select(id => id.Trim())
        .Distinct(StringComparer.Ordinal)
        .ToList();
}

public sealed class GoogleIdentityValidator : IGoogleIdentityValidator
{
    private readonly IReadOnlyList<string> _audiences;
    private readonly ILogger<GoogleIdentityValidator> _logger;

    public GoogleIdentityValidator(
        IOptions<GoogleAuthOptions> options,
        ILogger<GoogleIdentityValidator> logger)
    {
        _audiences = options.Value.Audiences();
        _logger = logger;
    }

    public bool IsConfigured => _audiences.Count > 0;

    public async Task<GoogleIdentity?> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(idToken))
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();

        GoogleJsonWebSignature.Payload payload;
        try
        {
            // Checks the Google signing key, the issuer and the expiry; Audience
            // pins the token to this app so a token minted for someone else's
            // client id cannot be replayed here.
            payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = _audiences });
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning(ex, "Google ID token rejected.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(payload.Subject) || string.IsNullOrWhiteSpace(payload.Email))
        {
            _logger.LogWarning("Google ID token carried no subject or email.");
            return null;
        }

        return new GoogleIdentity(
            payload.Subject,
            payload.Email.Trim(),
            payload.EmailVerified,
            string.IsNullOrWhiteSpace(payload.Name) ? null : payload.Name.Trim());
    }
}
