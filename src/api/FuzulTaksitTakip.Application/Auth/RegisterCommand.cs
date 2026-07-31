using FluentValidation;
using FuzulTaksitTakip.Application.Common.Interfaces;
using MediatR;

namespace FuzulTaksitTakip.Application.Auth;

public sealed record RegisterCommand(string Email, string Password, string DisplayName) : IRequest<RegisterResult>;

public sealed record RegisterResult(string UserId, string Email, string DisplayName);

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(128);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
    }
}

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResult>
{
    private readonly IIdentityService _identity;

    public RegisterCommandHandler(IIdentityService identity)
    {
        _identity = identity;
    }

    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var (succeeded, userId, errors) = await _identity.RegisterAsync(
            request.Email.Trim(),
            request.Password,
            request.DisplayName.Trim(),
            cancellationToken);

        if (!succeeded || userId is null)
        {
            throw new FluentValidation.ValidationException(
                errors.Select(e => new FluentValidation.Results.ValidationFailure(nameof(request.Email), e)));
        }

        return new RegisterResult(userId, request.Email.Trim(), request.DisplayName.Trim());
    }
}
