using FluentValidation;
using PayDefteri.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace PayDefteri.Application.Auth;

/// <summary>
/// Signs in — and, on a first visit, signs up — with the ID token Google's
/// button hands the client. One command covers both because Google is the only
/// thing that can tell the two cases apart.
/// </summary>
public sealed record GoogleLoginCommand(string IdToken, bool RememberMe = true) : IRequest<LoginResult>;

public sealed class GoogleLoginCommandValidator : AbstractValidator<GoogleLoginCommand>
{
    public GoogleLoginCommandValidator()
    {
        RuleFor(x => x.IdToken).NotEmpty().MaximumLength(4096);
    }
}

public sealed class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, LoginResult>
{
    private readonly IGoogleIdentityValidator _google;
    private readonly IIdentityService _identity;
    private readonly IJwtTokenService _jwt;
    private readonly ILogger<GoogleLoginCommandHandler> _logger;

    public GoogleLoginCommandHandler(
        IGoogleIdentityValidator google,
        IIdentityService identity,
        IJwtTokenService jwt,
        ILogger<GoogleLoginCommandHandler> logger)
    {
        _google = google;
        _identity = identity;
        _jwt = jwt;
        _logger = logger;
    }

    public async Task<LoginResult> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        var account = await GoogleSignIn.ResolveAsync(
            _google,
            _identity,
            request.IdToken,
            _logger,
            cancellationToken);

        var (token, expires) = _jwt.CreateToken(
            account.UserId,
            account.Email,
            account.DisplayName,
            account.IsSuperAdmin,
            request.RememberMe);
        return new LoginResult(token, expires);
    }
}

/// <summary>Account a verified Google token resolved to.</summary>
public sealed record GoogleAccount(
    string UserId,
    string Email,
    string DisplayName,
    bool IsSuperAdmin,
    bool Created);

/// <summary>
/// The token-to-account step, shared by the browser and the mobile endpoint so
/// both enforce the same checks.
/// </summary>
public static class GoogleSignIn
{
    public const string Provider = "Google";

    public static async Task<GoogleAccount> ResolveAsync(
        IGoogleIdentityValidator google,
        IIdentityService identity,
        string idToken,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!google.IsConfigured)
        {
            logger.LogError("Google sign-in attempted while Authentication:Google:ClientId is unset.");
            throw new UnauthorizedAccessException("Google ile giriş şu anda kullanılamıyor.");
        }

        var googleIdentity = await google.ValidateAsync(idToken, cancellationToken);
        if (googleIdentity is null)
        {
            throw new UnauthorizedAccessException("Google oturumu doğrulanamadı. Tekrar deneyin.");
        }

        // An unverified address must never reach the account lookup below: it
        // would let anyone claim someone else's e-mail and take over the account.
        if (!googleIdentity.EmailVerified)
        {
            logger.LogWarning("Google sign-in rejected: {Email} is not verified at Google.", googleIdentity.Email);
            throw new UnauthorizedAccessException("Google hesabınızın e-posta adresi doğrulanmamış.");
        }

        var email = googleIdentity.Email.Trim();
        var displayName = string.IsNullOrWhiteSpace(googleIdentity.Name)
            ? email.Split('@')[0]
            : googleIdentity.Name!;

        var (succeeded, userId, resolvedEmail, resolvedName, isSuperAdmin, created, errors) =
            await identity.FindOrCreateExternalUserAsync(
                Provider,
                googleIdentity.Subject,
                email,
                displayName,
                cancellationToken);

        if (!succeeded || userId is null || resolvedEmail is null)
        {
            logger.LogWarning(
                "Google sign-in could not resolve an account for {Email}: {Errors}",
                email,
                string.Join("; ", errors));
            throw new ValidationException(
                errors.Select(e => new FluentValidation.Results.ValidationFailure("IdToken", e)));
        }

        logger.LogInformation(
            "Google sign-in succeeded for {Email} (userId={UserId}, newAccount={Created})",
            resolvedEmail,
            userId,
            created);

        return new GoogleAccount(
            userId,
            resolvedEmail,
            string.IsNullOrWhiteSpace(resolvedName) ? displayName : resolvedName!,
            isSuperAdmin,
            created);
    }
}
