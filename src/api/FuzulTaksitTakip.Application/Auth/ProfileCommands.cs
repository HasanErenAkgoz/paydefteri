using FluentValidation;
using FuzulTaksitTakip.Application.Common.Exceptions;
using FuzulTaksitTakip.Application.Common.Interfaces;
using MediatR;

namespace FuzulTaksitTakip.Application.Auth;

public sealed record UserProfileDto(string UserId, string Email, string DisplayName);

public sealed record GetMyProfileQuery : IRequest<UserProfileDto>;

public sealed class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, UserProfileDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IIdentityService _identity;

    public GetMyProfileQueryHandler(ICurrentUser currentUser, IIdentityService identity)
    {
        _currentUser = currentUser;
        _identity = identity;
    }

    public async Task<UserProfileDto> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();
        var user = await _identity.FindByIdAsync(userId, cancellationToken);
        if (user.UserId is null || user.Email is null)
        {
            throw new NotFoundException("User", userId);
        }

        var displayName = string.IsNullOrWhiteSpace(user.DisplayName)
            ? (_currentUser.DisplayName ?? user.Email)
            : user.DisplayName!;

        return new UserProfileDto(user.UserId, user.Email, displayName);
    }
}

public sealed record UpdateProfileCommand(string DisplayName) : IRequest<LoginResult>;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
    }
}

public sealed class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, LoginResult>
{
    private readonly ICurrentUser _currentUser;
    private readonly IIdentityService _identity;
    private readonly IJwtTokenService _jwt;

    public UpdateProfileCommandHandler(ICurrentUser currentUser, IIdentityService identity, IJwtTokenService jwt)
    {
        _currentUser = currentUser;
        _identity = identity;
        _jwt = jwt;
    }

    public async Task<LoginResult> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();
        var (succeeded, errors) = await _identity.UpdateDisplayNameAsync(
            userId,
            request.DisplayName.Trim(),
            cancellationToken);

        if (!succeeded)
        {
            throw new FluentValidation.ValidationException(
                errors.Select(e => new FluentValidation.Results.ValidationFailure(nameof(request.DisplayName), e)));
        }

        var user = await _identity.FindByIdAsync(userId, cancellationToken);
        if (user.UserId is null || user.Email is null)
        {
            throw new NotFoundException("User", userId);
        }

        var (token, expires) = _jwt.CreateToken(
            user.UserId,
            user.Email,
            user.DisplayName ?? request.DisplayName.Trim());
        return new LoginResult(token, expires);
    }
}

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6).MaximumLength(128);
        RuleFor(x => x.NewPassword)
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("Yeni şifre mevcut şifre ile aynı olamaz.");
    }
}

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly ICurrentUser _currentUser;
    private readonly IIdentityService _identity;

    public ChangePasswordCommandHandler(ICurrentUser currentUser, IIdentityService identity)
    {
        _currentUser = currentUser;
        _identity = identity;
    }

    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();
        var (succeeded, errors) = await _identity.ChangePasswordAsync(
            userId,
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);

        if (!succeeded)
        {
            throw new FluentValidation.ValidationException(
                errors.Select(e => new FluentValidation.Results.ValidationFailure(nameof(request.CurrentPassword), e)));
        }
    }
}
