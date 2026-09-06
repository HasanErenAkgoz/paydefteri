using FluentValidation;
using PayDefteri.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace PayDefteri.Application.Auth;

public sealed record RegisterCommand(string Email, string Password, string DisplayName) : IRequest<LoginResult>;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(10).MaximumLength(128);
        RuleFor(x => x.Password).Matches("[A-Za-z]")
            .WithMessage("Şifre en az bir harf içermelidir.");
        RuleFor(x => x.Password).Matches("[0-9]")
            .WithMessage("Şifre en az bir rakam içermelidir.");
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
    }
}

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, LoginResult>
{
    private readonly IIdentityService _identity;
    private readonly IJwtTokenService _jwt;
    private readonly IEmailSender _emailSender;
    private readonly IEmailVerificationService _verification;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        IIdentityService identity,
        IJwtTokenService jwt,
        IEmailSender emailSender,
        IEmailVerificationService verification,
        ILogger<RegisterCommandHandler> logger)
    {
        _identity = identity;
        _jwt = jwt;
        _emailSender = emailSender;
        _verification = verification;
        _logger = logger;
    }

    public async Task<LoginResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var displayName = request.DisplayName.Trim();
        // Without a configured mail sender there is no way to verify an address,
        // so local and test setups keep the account usable straight away.
        var requiresVerification = _emailSender.IsConfigured;
        var (succeeded, userId, errors) = await _identity.RegisterAsync(
            email,
            request.Password,
            displayName,
            emailConfirmed: !requiresVerification,
            cancellationToken);

        if (!succeeded || userId is null)
        {
            _logger.LogWarning(
                "Register failed for {Email}: {Errors}",
                email,
                string.Join("; ", errors));
            throw new ValidationException(
                errors.Select(e => new FluentValidation.Results.ValidationFailure(nameof(request.Email), e)));
        }

        if (requiresVerification)
        {
            var (confirmationToken, confirmationEmail, _) =
                await _identity.CreateEmailConfirmationTokenAsync(userId, cancellationToken);
            if (confirmationToken is not null && confirmationEmail is not null)
            {
                await _verification.SendVerificationAsync(
                    new EmailVerificationRequest(confirmationEmail, displayName, userId, confirmationToken),
                    cancellationToken);
            }
        }

        var (token, expires) = _jwt.CreateToken(userId, email, displayName, isSuperAdmin: false);
        _logger.LogInformation("Register succeeded for {Email} (userId={UserId})", email, userId);
        return new LoginResult(token, expires);
    }
}
