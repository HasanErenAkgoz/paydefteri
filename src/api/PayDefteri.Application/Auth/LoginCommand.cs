using FluentValidation;
using PayDefteri.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace PayDefteri.Application.Auth;

public sealed record LoginCommand(string Email, string Password) : IRequest<LoginResult>;

public sealed record LoginResult(string AccessToken, DateTime ExpiresAt);

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IIdentityService _identity;
    private readonly IJwtTokenService _jwt;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IIdentityService identity,
        IJwtTokenService jwt,
        ILogger<LoginCommandHandler> logger)
    {
        _identity = identity;
        _jwt = jwt;
        _logger = logger;
    }

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var (succeeded, userId, resolvedEmail, displayName, isSuperAdmin) = await _identity.ValidateCredentialsAsync(
            email,
            request.Password,
            cancellationToken);

        if (!succeeded || userId is null || resolvedEmail is null)
        {
            _logger.LogWarning("Login failed for {Email}", email);
            throw new UnauthorizedAccessException("E-posta veya şifre hatalı.");
        }

        var (token, expires) = _jwt.CreateToken(
            userId,
            resolvedEmail,
            displayName ?? string.Empty,
            isSuperAdmin);
        _logger.LogInformation(
            "Login succeeded for {Email} (userId={UserId}, displayName={DisplayName}, superAdmin={IsSuperAdmin})",
            resolvedEmail,
            userId,
            displayName,
            isSuperAdmin);
        return new LoginResult(token, expires);
    }
}
