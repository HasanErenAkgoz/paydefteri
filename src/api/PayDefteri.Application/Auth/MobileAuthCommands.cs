using FluentValidation;
using PayDefteri.Application.Common.Exceptions;
using PayDefteri.Application.Common.Interfaces;
using PayDefteri.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace PayDefteri.Application.Auth;

public sealed record MobileDeviceInfo(string DeviceName, string Platform, string AppVersion);

public sealed record MobileAuthResult(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    Guid SessionId,
    UserProfileDto User);

public sealed record MobileSessionDto(
    Guid Id,
    string DeviceName,
    string Platform,
    string AppVersion,
    DateTime CreatedAtUtc,
    DateTime? LastUsedAtUtc,
    DateTime ExpiresAtUtc,
    bool IsCurrent);

public sealed record MobileLoginCommand(
    string Email,
    string Password,
    MobileDeviceInfo Device) : IRequest<MobileAuthResult>;

public sealed class MobileLoginCommandValidator : AbstractValidator<MobileLoginCommand>
{
    public MobileLoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Device).NotNull().SetValidator(new MobileDeviceInfoValidator());
    }
}

public sealed record MobileRegisterCommand(
    string Email,
    string Password,
    string DisplayName,
    MobileDeviceInfo Device) : IRequest<MobileAuthResult>;

public sealed class MobileRegisterCommandValidator : AbstractValidator<MobileRegisterCommand>
{
    public MobileRegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(10).MaximumLength(128)
            .Matches("[A-Za-z]").WithMessage("Şifre en az bir harf içermelidir.")
            .Matches("[0-9]").WithMessage("Şifre en az bir rakam içermelidir.");
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Device).NotNull().SetValidator(new MobileDeviceInfoValidator());
    }
}

public sealed class MobileDeviceInfoValidator : AbstractValidator<MobileDeviceInfo>
{
    public MobileDeviceInfoValidator()
    {
        RuleFor(x => x.DeviceName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Platform).NotEmpty().MaximumLength(32);
        RuleFor(x => x.AppVersion).NotEmpty().MaximumLength(32);
    }
}

public sealed class MobileLoginCommandHandler : IRequestHandler<MobileLoginCommand, MobileAuthResult>
{
    private readonly IIdentityService _identity;
    private readonly MobileSessionIssuer _issuer;
    private readonly ILogger<MobileLoginCommandHandler> _logger;

    public MobileLoginCommandHandler(
        IIdentityService identity,
        MobileSessionIssuer issuer,
        ILogger<MobileLoginCommandHandler> logger)
    {
        _identity = identity;
        _issuer = issuer;
        _logger = logger;
    }

    public async Task<MobileAuthResult> Handle(MobileLoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var identity = await _identity.ValidateCredentialsAsync(email, request.Password, cancellationToken);
        if (!identity.Succeeded || identity.UserId is null || identity.Email is null)
        {
            _logger.LogWarning("Mobile login failed for {Email}", email);
            throw new UnauthorizedAccessException("E-posta veya şifre hatalı.");
        }

        return await _issuer.IssueAsync(
            identity.UserId,
            identity.Email,
            identity.DisplayName ?? identity.Email,
            identity.IsSuperAdmin,
            request.Device,
            familyId: null,
            cancellationToken);
    }
}

public sealed class MobileRegisterCommandHandler : IRequestHandler<MobileRegisterCommand, MobileAuthResult>
{
    private readonly IIdentityService _identity;
    private readonly MobileSessionIssuer _issuer;

    public MobileRegisterCommandHandler(IIdentityService identity, MobileSessionIssuer issuer)
    {
        _identity = identity;
        _issuer = issuer;
    }

    public async Task<MobileAuthResult> Handle(MobileRegisterCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var displayName = request.DisplayName.Trim();
        var registration = await _identity.RegisterAsync(email, request.Password, displayName, cancellationToken);
        if (!registration.Succeeded || registration.UserId is null)
        {
            throw new ValidationException(registration.Errors.Select(error =>
                new FluentValidation.Results.ValidationFailure(nameof(request.Email), error)));
        }

        return await _issuer.IssueAsync(
            registration.UserId,
            email,
            displayName,
            isSuperAdmin: false,
            request.Device,
            familyId: null,
            cancellationToken);
    }
}

public sealed record RefreshMobileSessionCommand(string RefreshToken) : IRequest<MobileAuthResult>;

public sealed class RefreshMobileSessionCommandValidator : AbstractValidator<RefreshMobileSessionCommand>
{
    public RefreshMobileSessionCommandValidator() =>
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(256);
}

public sealed class RefreshMobileSessionCommandHandler
    : IRequestHandler<RefreshMobileSessionCommand, MobileAuthResult>
{
    private readonly IAppDbContext _db;
    private readonly IIdentityService _identity;
    private readonly IMobileRefreshTokenService _tokens;
    private readonly MobileSessionIssuer _issuer;

    public RefreshMobileSessionCommandHandler(
        IAppDbContext db,
        IIdentityService identity,
        IMobileRefreshTokenService tokens,
        MobileSessionIssuer issuer)
    {
        _db = db;
        _identity = identity;
        _tokens = tokens;
        _issuer = issuer;
    }

    public async Task<MobileAuthResult> Handle(
        RefreshMobileSessionCommand request,
        CancellationToken cancellationToken)
    {
        var tokenHash = _tokens.HashToken(request.RefreshToken);
        var session = await _db.MobileRefreshSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        if (session is null)
        {
            throw new UnauthorizedAccessException("Mobil oturum geçersiz.");
        }

        var now = DateTime.UtcNow;
        if (!session.IsActive(now))
        {
            if (session.RevokedAtUtc is not null)
            {
                var family = await _db.MobileRefreshSessions
                    .Where(x => x.FamilyId == session.FamilyId && x.RevokedAtUtc == null)
                    .ToListAsync(cancellationToken);
                foreach (var member in family)
                {
                    member.RevokedAtUtc = now;
                }
                await _db.SaveChangesAsync(cancellationToken);
            }

            throw new UnauthorizedAccessException("Mobil oturumun süresi dolmuş veya oturum iptal edilmiş.");
        }

        var user = await _identity.FindSessionUserByIdAsync(session.UserId, cancellationToken);
        if (user.UserId is null || user.Email is null)
        {
            var invalidSession = await _db.MobileRefreshSessions.SingleAsync(
                x => x.Id == session.Id,
                cancellationToken);
            invalidSession.RevokedAtUtc ??= now;
            await _db.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException("Mobil oturum kullanıcısı bulunamadı.");
        }

        var replacementId = Guid.NewGuid();
        var result = await _db.ExecuteInTransactionAsync<MobileAuthResult?>(async ct =>
        {
            var claimed = await _db.TryClaimMobileRefreshSessionAsync(
                session.Id,
                now,
                replacementId,
                ct);
            if (!claimed)
            {
                return null;
            }

            var issued = await _issuer.IssueAsync(
                user.UserId,
                user.Email,
                user.DisplayName ?? user.Email,
                user.IsSuperAdmin,
                new MobileDeviceInfo(session.DeviceName, session.Platform, session.AppVersion),
                session.FamilyId,
                ct,
                saveChanges: false,
                sessionId: replacementId);
            await _db.SaveChangesAsync(ct);
            return issued;
        }, cancellationToken);

        if (result is not null)
        {
            return result;
        }

        var activeFamily = await _db.MobileRefreshSessions
            .Where(x => x.FamilyId == session.FamilyId && x.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var member in activeFamily)
        {
            member.RevokedAtUtc = now;
        }
        await _db.SaveChangesAsync(cancellationToken);
        throw new UnauthorizedAccessException("Mobil oturum daha önce yenilenmiş.");
    }
}

public sealed record LogoutMobileSessionCommand(string RefreshToken) : IRequest;

public sealed class LogoutMobileSessionCommandHandler : IRequestHandler<LogoutMobileSessionCommand>
{
    private readonly IAppDbContext _db;
    private readonly IMobileRefreshTokenService _tokens;

    public LogoutMobileSessionCommandHandler(IAppDbContext db, IMobileRefreshTokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    public async Task Handle(LogoutMobileSessionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken) || request.RefreshToken.Length > 256)
        {
            return;
        }

        var hash = _tokens.HashToken(request.RefreshToken);
        var session = await _db.MobileRefreshSessions.SingleOrDefaultAsync(
            x => x.TokenHash == hash,
            cancellationToken);
        if (session is not null && session.RevokedAtUtc is null)
        {
            session.RevokedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}

public sealed record ListMobileSessionsQuery(Guid? CurrentSessionId) : IRequest<IReadOnlyList<MobileSessionDto>>;

public sealed class ListMobileSessionsQueryHandler
    : IRequestHandler<ListMobileSessionsQuery, IReadOnlyList<MobileSessionDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListMobileSessionsQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<MobileSessionDto>> Handle(
        ListMobileSessionsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();
        var now = DateTime.UtcNow;
        return await _db.MobileRefreshSessions.AsNoTracking()
            .Where(x => x.UserId == userId && x.RevokedAtUtc == null && x.ExpiresAtUtc > now)
            .OrderByDescending(x => x.LastUsedAtUtc ?? x.CreatedAtUtc)
            .Select(x => new MobileSessionDto(
                x.Id,
                x.DeviceName,
                x.Platform,
                x.AppVersion,
                x.CreatedAtUtc,
                x.LastUsedAtUtc,
                x.ExpiresAtUtc,
                request.CurrentSessionId == x.Id))
            .ToListAsync(cancellationToken);
    }
}

public sealed record RevokeMobileSessionCommand(Guid SessionId) : IRequest;

public sealed class RevokeMobileSessionCommandHandler : IRequestHandler<RevokeMobileSessionCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public RevokeMobileSessionCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(RevokeMobileSessionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();
        var session = await _db.MobileRefreshSessions.SingleOrDefaultAsync(
            x => x.Id == request.SessionId && x.UserId == userId,
            cancellationToken) ?? throw new NotFoundException("MobileSession", request.SessionId);
        session.RevokedAtUtc ??= DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class MobileSessionIssuer
{
    private readonly IAppDbContext _db;
    private readonly IJwtTokenService _jwt;
    private readonly IMobileRefreshTokenService _tokens;

    public MobileSessionIssuer(IAppDbContext db, IJwtTokenService jwt, IMobileRefreshTokenService tokens)
    {
        _db = db;
        _jwt = jwt;
        _tokens = tokens;
    }

    public async Task<MobileAuthResult> IssueAsync(
        string userId,
        string email,
        string displayName,
        bool isSuperAdmin,
        MobileDeviceInfo device,
        Guid? familyId,
        CancellationToken cancellationToken,
        bool saveChanges = true,
        Guid? sessionId = null)
    {
        var refreshToken = _tokens.CreateToken();
        var session = new MobileRefreshSession
        {
            Id = sessionId ?? Guid.NewGuid(),
            UserId = userId,
            TokenHash = _tokens.HashToken(refreshToken),
            FamilyId = familyId ?? Guid.NewGuid(),
            DeviceName = device.DeviceName.Trim(),
            Platform = device.Platform.Trim().ToLowerInvariant(),
            AppVersion = device.AppVersion.Trim(),
            ExpiresAtUtc = _tokens.GetExpiryUtc(),
        };
        _db.MobileRefreshSessions.Add(session);
        if (saveChanges)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        var access = _jwt.CreateToken(userId, email, displayName, isSuperAdmin);
        return new MobileAuthResult(
            access.AccessToken,
            access.ExpiresAtUtc,
            refreshToken,
            session.ExpiresAtUtc,
            session.Id,
            new UserProfileDto(userId, email, displayName));
    }
}
