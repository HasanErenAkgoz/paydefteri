using FluentValidation;
using PayDefteri.Application.Common.Exceptions;
using PayDefteri.Application.Common.Interfaces;
using PayDefteri.Application.Common.Mapping;
using PayDefteri.Application.Common.Models;
using PayDefteri.Domain.Entities;
using PayDefteri.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace PayDefteri.Application.Membership;

public sealed record ListMembersQuery(Guid PlanId) : IRequest<IReadOnlyList<PlanMemberDto>>;

public sealed class ListMembersQueryHandler : IRequestHandler<ListMembersQuery, IReadOnlyList<PlanMemberDto>>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;
    private readonly IIdentityService _identity;
    private readonly ICurrentUser _currentUser;

    public ListMembersQueryHandler(
        IAppDbContext db,
        IPlanAuthorization auth,
        IIdentityService identity,
        ICurrentUser currentUser)
    {
        _db = db;
        _auth = auth;
        _identity = identity;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<PlanMemberDto>> Handle(ListMembersQuery request, CancellationToken cancellationToken)
    {
        await _auth.EnsureMemberAsync(request.PlanId, cancellationToken);

        var members = await _db.PlanMembers.AsNoTracking()
            .Include(m => m.Partner)
            .Where(m => m.PlanId == request.PlanId)
            .OrderBy(m => m.Role)
            .ThenBy(m => m.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var result = new List<PlanMemberDto>();
        foreach (var m in members)
        {
            var user = await _identity.FindByIdAsync(m.UserId, cancellationToken);
            var email = user.Email;
            var displayName = string.IsNullOrWhiteSpace(user.DisplayName) ? null : user.DisplayName.Trim();

            // JWT still valid after AspNetUsers wipe / orphaned member rows.
            if (string.Equals(m.UserId, _currentUser.UserId, StringComparison.Ordinal)
                && (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(displayName)))
            {
                email ??= _currentUser.Email;
                displayName ??= string.IsNullOrWhiteSpace(_currentUser.DisplayName)
                    ? null
                    : _currentUser.DisplayName.Trim();
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = !string.IsNullOrWhiteSpace(email)
                    ? email
                    : m.Role == PlanMemberRole.Owner
                        ? "Plan sahibi"
                        : "Üye";
            }

            result.Add(new PlanMemberDto(
                m.Id,
                m.UserId,
                email,
                displayName,
                m.Role.ToString(),
                m.PartnerId,
                m.Partner?.Name));
        }

        return result;
    }
}

public sealed record ListInvitesQuery(Guid PlanId) : IRequest<IReadOnlyList<PlanInviteDto>>;

public sealed class ListInvitesQueryHandler : IRequestHandler<ListInvitesQuery, IReadOnlyList<PlanInviteDto>>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;

    public ListInvitesQueryHandler(IAppDbContext db, IPlanAuthorization auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task<IReadOnlyList<PlanInviteDto>> Handle(ListInvitesQuery request, CancellationToken cancellationToken)
    {
        await _auth.EnsureOwnerAsync(request.PlanId, cancellationToken);

        return await _db.PlanInvites.AsNoTracking()
            .Include(i => i.Partner)
            .Where(i => i.PlanId == request.PlanId && i.Status == PlanInviteStatus.Pending)
            .OrderByDescending(i => i.CreatedAtUtc)
            .Select(i => new PlanInviteDto(
                i.Id,
                i.Email,
                i.PartnerId,
                i.Partner.Name,
                i.Status.ToString(),
                i.Token,
                i.ExpiresAtUtc,
                i.CreatedAtUtc,
                false))
            .ToListAsync(cancellationToken);
    }
}

public sealed record CreateInviteCommand(Guid PlanId, string Email, Guid PartnerId) : IRequest<PlanInviteDto>;

public sealed class CreateInviteCommandValidator : AbstractValidator<CreateInviteCommand>
{
    public CreateInviteCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}

public sealed class CreateInviteCommandHandler : IRequestHandler<CreateInviteCommand, PlanInviteDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;
    private readonly ICurrentUser _currentUser;
    private readonly IIdentityService _identity;
    private readonly IInviteEmailService _inviteEmail;

    public CreateInviteCommandHandler(
        IAppDbContext db,
        IPlanAuthorization auth,
        ICurrentUser currentUser,
        IIdentityService identity,
        IInviteEmailService inviteEmail)
    {
        _db = db;
        _auth = auth;
        _currentUser = currentUser;
        _identity = identity;
        _inviteEmail = inviteEmail;
    }

    public async Task<PlanInviteDto> Handle(CreateInviteCommand request, CancellationToken cancellationToken)
    {
        await _auth.EnsureOwnerAsync(request.PlanId, cancellationToken);
        var inviterId = _currentUser.UserId ?? throw new ForbiddenException();

        var email = request.Email.Trim().ToLowerInvariant();
        var partner = await _db.Partners
            .FirstOrDefaultAsync(p => p.Id == request.PartnerId && p.PlanId == request.PlanId, cancellationToken)
            ?? throw new NotFoundException(nameof(Partner), request.PartnerId);

        var plan = await _db.Plans
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken)
            ?? throw new NotFoundException(nameof(Plan), request.PlanId);

        if (!string.IsNullOrEmpty(partner.LinkedUserId))
        {
            throw new ConflictException("Bu ortak zaten bir kullanıcıya bağlı.");
        }

        var existingUser = await _identity.FindByEmailAsync(email, cancellationToken);
        if (existingUser.UserId is not null)
        {
            var alreadyMember = await _db.PlanMembers.AnyAsync(
                m => m.PlanId == request.PlanId && m.UserId == existingUser.UserId,
                cancellationToken);
            if (alreadyMember)
            {
                throw new ConflictException("Kullanıcı zaten plan üyesi.");
            }
        }

        var pending = await _db.PlanInvites
            .Where(i => i.PlanId == request.PlanId
                        && i.Email == email
                        && i.Status == PlanInviteStatus.Pending)
            .ToListAsync(cancellationToken);
        foreach (var old in pending)
        {
            old.Status = PlanInviteStatus.Revoked;
            old.UpdatedAtUtc = DateTime.UtcNow;
        }

        var invite = new PlanInvite
        {
            PlanId = request.PlanId,
            Email = email,
            PartnerId = partner.Id,
            Token = Convert.ToHexString(Guid.NewGuid().ToByteArray()) + Convert.ToHexString(Guid.NewGuid().ToByteArray()),
            InvitedByUserId = inviterId,
            Status = PlanInviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(14)
        };

        _db.PlanInvites.Add(invite);
        await _db.SaveChangesAsync(cancellationToken);

        var inviter = await _identity.FindByIdAsync(inviterId, cancellationToken);
        var mailResult = await _inviteEmail.SendInviteAsync(
            new InviteEmailRequest(
                email,
                plan.Title,
                partner.Name,
                inviter.DisplayName ?? inviter.Email ?? "Bir kullanıcı",
                invite.Token,
                invite.ExpiresAtUtc),
            cancellationToken);

        return new PlanInviteDto(
            invite.Id,
            invite.Email,
            invite.PartnerId,
            partner.Name,
            invite.Status.ToString(),
            invite.Token,
            invite.ExpiresAtUtc,
            invite.CreatedAtUtc,
            mailResult.Sent);
    }
}

public sealed record GetInvitePreviewQuery(string Token) : IRequest<InvitePreviewDto>;

public sealed class GetInvitePreviewQueryHandler : IRequestHandler<GetInvitePreviewQuery, InvitePreviewDto>
{
    private readonly IAppDbContext _db;
    private readonly IIdentityService _identity;

    public GetInvitePreviewQueryHandler(IAppDbContext db, IIdentityService identity)
    {
        _db = db;
        _identity = identity;
    }

    public async Task<InvitePreviewDto> Handle(GetInvitePreviewQuery request, CancellationToken cancellationToken)
    {
        var invite = await _db.PlanInvites.AsNoTracking()
            .Include(i => i.Partner)
            .Include(i => i.Plan)
            .FirstOrDefaultAsync(i => i.Token == request.Token, cancellationToken)
            ?? throw new NotFoundException(nameof(PlanInvite), request.Token);

        var isAcceptable = invite.Status == PlanInviteStatus.Pending
            && invite.ExpiresAtUtc >= DateTime.UtcNow;

        var existing = await _identity.FindByEmailAsync(invite.Email, cancellationToken);
        var accountExists = existing.UserId is not null;

        return new InvitePreviewDto(
            invite.Token,
            invite.Email,
            invite.Partner.Name,
            invite.Plan.Title,
            invite.Status.ToString(),
            invite.ExpiresAtUtc,
            isAcceptable,
            accountExists);
    }
}

public sealed record AcceptInviteCommand(string Token) : IRequest<PlanDto>;

public sealed class AcceptInviteCommandHandler : IRequestHandler<AcceptInviteCommand, PlanDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IIdentityService _identity;

    public AcceptInviteCommandHandler(IAppDbContext db, ICurrentUser currentUser, IIdentityService identity)
    {
        _db = db;
        _currentUser = currentUser;
        _identity = identity;
    }

    public async Task<PlanDto> Handle(AcceptInviteCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();
        var me = await _identity.FindByIdAsync(userId, cancellationToken);
        if (me.Email is null)
        {
            throw new ForbiddenException();
        }

        var invite = await _db.PlanInvites
            .Include(i => i.Partner)
            .FirstOrDefaultAsync(i => i.Token == request.Token, cancellationToken)
            ?? throw new NotFoundException(nameof(PlanInvite), request.Token);

        if (!string.Equals(invite.Email, me.Email, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException("Bu davet başka bir e-posta adresine ait.");
        }

        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == invite.PlanId, cancellationToken)
            ?? throw new NotFoundException(nameof(Plan), invite.PlanId);

        // Idempotent: same user already accepted — return plan instead of a confusing conflict.
        if (invite.Status == PlanInviteStatus.Accepted)
        {
            var isMember = await _db.PlanMembers.AnyAsync(
                m => m.PlanId == plan.Id && m.UserId == userId, cancellationToken);
            if (isMember)
            {
                return plan.ToDto();
            }

            throw new ConflictException("Bu davet başka bir hesap tarafından kabul edilmiş.");
        }

        if (invite.Status != PlanInviteStatus.Pending || invite.ExpiresAtUtc < DateTime.UtcNow)
        {
            throw new ConflictException("Davet geçersiz veya süresi dolmuş.");
        }

        var already = await _db.PlanMembers.AnyAsync(
            m => m.PlanId == plan.Id && m.UserId == userId, cancellationToken);
        if (!already)
        {
            _db.PlanMembers.Add(new PlanMember
            {
                PlanId = plan.Id,
                UserId = userId,
                Role = PlanMemberRole.Member,
                PartnerId = invite.PartnerId
            });
        }

        if (!string.IsNullOrEmpty(invite.Partner.LinkedUserId)
            && !string.Equals(invite.Partner.LinkedUserId, userId, StringComparison.Ordinal))
        {
            throw new ConflictException("Bu ortak başka bir kullanıcıya bağlı.");
        }

        invite.Partner.LinkedUserId = userId;
        invite.Status = PlanInviteStatus.Accepted;
        invite.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return plan.ToDto();
    }
}

public sealed record RevokeInviteCommand(Guid PlanId, Guid InviteId) : IRequest;

public sealed class RevokeInviteCommandHandler : IRequestHandler<RevokeInviteCommand>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;

    public RevokeInviteCommandHandler(IAppDbContext db, IPlanAuthorization auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task Handle(RevokeInviteCommand request, CancellationToken cancellationToken)
    {
        await _auth.EnsureOwnerAsync(request.PlanId, cancellationToken);
        var invite = await _db.PlanInvites
            .FirstOrDefaultAsync(i => i.Id == request.InviteId && i.PlanId == request.PlanId, cancellationToken)
            ?? throw new NotFoundException(nameof(PlanInvite), request.InviteId);

        invite.Status = PlanInviteStatus.Revoked;
        invite.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public sealed record ResendInviteCommand(Guid PlanId, Guid InviteId) : IRequest<PlanInviteDto>;

public sealed class ResendInviteCommandHandler : IRequestHandler<ResendInviteCommand, PlanInviteDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;
    private readonly ICurrentUser _currentUser;
    private readonly IIdentityService _identity;
    private readonly IInviteEmailService _inviteEmail;

    public ResendInviteCommandHandler(
        IAppDbContext db,
        IPlanAuthorization auth,
        ICurrentUser currentUser,
        IIdentityService identity,
        IInviteEmailService inviteEmail)
    {
        _db = db;
        _auth = auth;
        _currentUser = currentUser;
        _identity = identity;
        _inviteEmail = inviteEmail;
    }

    public async Task<PlanInviteDto> Handle(ResendInviteCommand request, CancellationToken cancellationToken)
    {
        await _auth.EnsureOwnerAsync(request.PlanId, cancellationToken);
        var inviterId = _currentUser.UserId ?? throw new ForbiddenException();

        var invite = await _db.PlanInvites
            .Include(i => i.Partner)
            .Include(i => i.Plan)
            .FirstOrDefaultAsync(i => i.Id == request.InviteId && i.PlanId == request.PlanId, cancellationToken)
            ?? throw new NotFoundException(nameof(PlanInvite), request.InviteId);

        if (invite.Status != PlanInviteStatus.Pending)
        {
            throw new ConflictException("Yalnızca bekleyen davetler yeniden gönderilebilir.");
        }

        if (invite.ExpiresAtUtc < DateTime.UtcNow)
        {
            invite.Token = Convert.ToHexString(Guid.NewGuid().ToByteArray())
                + Convert.ToHexString(Guid.NewGuid().ToByteArray());
            invite.ExpiresAtUtc = DateTime.UtcNow.AddDays(14);
        }

        invite.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        var inviter = await _identity.FindByIdAsync(inviterId, cancellationToken);
        var mailResult = await _inviteEmail.SendInviteAsync(
            new InviteEmailRequest(
                invite.Email,
                invite.Plan.Title,
                invite.Partner.Name,
                inviter.DisplayName ?? inviter.Email ?? "Bir kullanıcı",
                invite.Token,
                invite.ExpiresAtUtc),
            cancellationToken);

        return new PlanInviteDto(
            invite.Id,
            invite.Email,
            invite.PartnerId,
            invite.Partner.Name,
            invite.Status.ToString(),
            invite.Token,
            invite.ExpiresAtUtc,
            invite.CreatedAtUtc,
            mailResult.Sent);
    }
}

public sealed record ListMyPendingInvitesQuery : IRequest<IReadOnlyList<PlanInviteDto>>;

public sealed class ListMyPendingInvitesQueryHandler : IRequestHandler<ListMyPendingInvitesQuery, IReadOnlyList<PlanInviteDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IIdentityService _identity;

    public ListMyPendingInvitesQueryHandler(IAppDbContext db, ICurrentUser currentUser, IIdentityService identity)
    {
        _db = db;
        _currentUser = currentUser;
        _identity = identity;
    }

    public async Task<IReadOnlyList<PlanInviteDto>> Handle(ListMyPendingInvitesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();
        var me = await _identity.FindByIdAsync(userId, cancellationToken);
        if (me.Email is null)
        {
            return Array.Empty<PlanInviteDto>();
        }

        var email = me.Email.ToLowerInvariant();
        return await _db.PlanInvites.AsNoTracking()
            .Include(i => i.Partner)
            .Where(i => i.Email == email
                        && i.Status == PlanInviteStatus.Pending
                        && i.ExpiresAtUtc >= DateTime.UtcNow)
            .OrderByDescending(i => i.CreatedAtUtc)
            .Select(i => new PlanInviteDto(
                i.Id,
                i.Email,
                i.PartnerId,
                i.Partner.Name,
                i.Status.ToString(),
                i.Token,
                i.ExpiresAtUtc,
                i.CreatedAtUtc,
                false))
            .ToListAsync(cancellationToken);
    }
}

public sealed record LinkSelfToPartnerCommand(Guid PlanId, Guid PartnerId) : IRequest<PartnerDto>;

public sealed class LinkSelfToPartnerCommandHandler : IRequestHandler<LinkSelfToPartnerCommand, PartnerDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;
    private readonly ICurrentUser _currentUser;
    private readonly IIdentityService _identity;

    public LinkSelfToPartnerCommandHandler(
        IAppDbContext db,
        IPlanAuthorization auth,
        ICurrentUser currentUser,
        IIdentityService identity)
    {
        _db = db;
        _auth = auth;
        _currentUser = currentUser;
        _identity = identity;
    }

    public async Task<PartnerDto> Handle(LinkSelfToPartnerCommand request, CancellationToken cancellationToken)
    {
        await _auth.EnsureMemberAsync(request.PlanId, cancellationToken);
        var userId = _currentUser.UserId ?? throw new ForbiddenException();

        var already = await _db.Partners.AnyAsync(
            p => p.PlanId == request.PlanId && p.LinkedUserId == userId,
            cancellationToken);
        if (already)
        {
            throw new ConflictException("Zaten bir ortağa bağlısınız.");
        }

        var partner = await _db.Partners
            .FirstOrDefaultAsync(p => p.Id == request.PartnerId && p.PlanId == request.PlanId, cancellationToken)
            ?? throw new NotFoundException(nameof(Partner), request.PartnerId);

        if (!string.IsNullOrEmpty(partner.LinkedUserId))
        {
            throw new ConflictException("Bu ortak başka bir kullanıcıya bağlı.");
        }

        partner.LinkedUserId = userId;
        partner.UpdatedAtUtc = DateTime.UtcNow;

        var user = await _identity.FindByIdAsync(userId, cancellationToken);
        var displayName = _currentUser.DisplayName?.Trim();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = user.DisplayName?.Trim();
        }

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            partner.Name = displayName;
        }

        var email = (_currentUser.Email ?? user.Email)?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(email))
        {
            var others = await _db.Partners
                .Where(p => p.PlanId == request.PlanId
                            && p.Id != partner.Id
                            && p.InviteEmail == email)
                .ToListAsync(cancellationToken);
            foreach (var other in others)
            {
                other.InviteEmail = null;
                other.UpdatedAtUtc = DateTime.UtcNow;
            }

            partner.InviteEmail = email;
        }

        var member = await _db.PlanMembers
            .FirstOrDefaultAsync(m => m.PlanId == request.PlanId && m.UserId == userId, cancellationToken);
        if (member is not null)
        {
            member.PartnerId = partner.Id;
            member.UpdatedAtUtc = DateTime.UtcNow;
        }
        else if (await _auth.IsOwnerAsync(request.PlanId, cancellationToken))
        {
            _db.PlanMembers.Add(new PlanMember
            {
                PlanId = request.PlanId,
                UserId = userId,
                Role = PlanMemberRole.Owner,
                PartnerId = partner.Id
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        return partner.ToDto();
    }
}
