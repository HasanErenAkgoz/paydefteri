using FluentValidation;
using MediatR;
using PayDefteri.Application.Common.Exceptions;
using PayDefteri.Application.Common.Interfaces;

namespace PayDefteri.Application.Auth;

/// <summary>Confirms an address from the link in the verification email.</summary>
public sealed record ConfirmEmailCommand(string UserId, string Token) : IRequest;

public sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Token).NotEmpty();
    }
}

public sealed class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand>
{
    private readonly IIdentityService _identity;

    public ConfirmEmailCommandHandler(IIdentityService identity) => _identity = identity;

    public async Task Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var (succeeded, errors) = await _identity.ConfirmEmailAsync(
            request.UserId,
            request.Token,
            cancellationToken);

        if (!succeeded)
        {
            throw new ValidationException(
                errors.Select(e => new FluentValidation.Results.ValidationFailure(nameof(request.Token), e)));
        }
    }
}

public sealed record ResendEmailVerificationCommand : IRequest<EmailVerificationResult>;

public sealed class ResendEmailVerificationCommandHandler
    : IRequestHandler<ResendEmailVerificationCommand, EmailVerificationResult>
{
    private readonly ICurrentUser _currentUser;
    private readonly IIdentityService _identity;
    private readonly IEmailVerificationService _verification;

    public ResendEmailVerificationCommandHandler(
        ICurrentUser currentUser,
        IIdentityService identity,
        IEmailVerificationService verification)
    {
        _currentUser = currentUser;
        _identity = identity;
        _verification = verification;
    }

    public async Task<EmailVerificationResult> Handle(
        ResendEmailVerificationCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();

        if (await _identity.IsEmailConfirmedAsync(userId, cancellationToken))
        {
            // Already verified: nothing to send, and saying so leaks nothing
            // because the caller is the account owner.
            return new EmailVerificationResult(Sent: false, Configured: true);
        }

        var (token, email, displayName) =
            await _identity.CreateEmailConfirmationTokenAsync(userId, cancellationToken);
        if (token is null || email is null)
        {
            throw new NotFoundException("User", userId);
        }

        return await _verification.SendVerificationAsync(
            new EmailVerificationRequest(email, displayName ?? email, userId, token),
            cancellationToken);
    }
}
