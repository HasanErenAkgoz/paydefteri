namespace PayDefteri.Application.Common.Interfaces;

/// <summary>What a verified Google ID token says about the person signing in.</summary>
public sealed record GoogleIdentity(string Subject, string Email, bool EmailVerified, string? Name);

/// <summary>
/// Verifies the ID token the Google sign-in button hands the client. The token
/// is the only proof of identity in this flow, so it is checked server side —
/// signature, issuer, expiry and audience — before any account is touched.
/// </summary>
public interface IGoogleIdentityValidator
{
    /// <summary>False when no client id is configured, so callers can answer with a clear error.</summary>
    bool IsConfigured { get; }

    /// <summary>Null when the token is malformed, expired, forged or issued for another app.</summary>
    Task<GoogleIdentity?> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
}
