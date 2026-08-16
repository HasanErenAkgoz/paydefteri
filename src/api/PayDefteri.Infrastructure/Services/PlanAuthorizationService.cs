using PayDefteri.Application.Common.Exceptions;
using PayDefteri.Application.Common.Interfaces;
using PayDefteri.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace PayDefteri.Infrastructure.Services;

public sealed class PlanAuthorizationService : IPlanAuthorization
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public PlanAuthorizationService(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task EnsureOwnerAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        RequireUserId();
        if (_currentUser.IsSuperAdmin)
        {
            _ = await _db.Plans.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken)
                ?? throw new NotFoundException(nameof(Plan), planId);
            return;
        }

        var userId = RequireUserId();
        var plan = await _db.Plans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken)
            ?? throw new NotFoundException(nameof(Plan), planId);

        if (!string.Equals(plan.OwnerUserId, userId, StringComparison.Ordinal))
        {
            throw new ForbiddenException();
        }
    }

    public Task EnsureOwnerAsync(Plan plan, CancellationToken cancellationToken = default)
    {
        RequireUserId();
        if (_currentUser.IsSuperAdmin)
        {
            return Task.CompletedTask;
        }

        var userId = RequireUserId();
        if (!string.Equals(plan.OwnerUserId, userId, StringComparison.Ordinal))
        {
            throw new ForbiddenException();
        }

        return Task.CompletedTask;
    }

    public async Task EnsureMemberAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        RequireUserId();
        if (_currentUser.IsSuperAdmin)
        {
            _ = await _db.Plans.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == planId && !p.IsDeleted, cancellationToken)
                ?? throw new NotFoundException(nameof(Plan), planId);
            return;
        }

        var userId = RequireUserId();
        var plan = await _db.Plans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == planId && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Plan), planId);

        if (string.Equals(plan.OwnerUserId, userId, StringComparison.Ordinal))
        {
            return;
        }

        var isMember = await _db.PlanMembers.AsNoTracking()
            .AnyAsync(m => m.PlanId == planId && m.UserId == userId, cancellationToken);

        if (!isMember)
        {
            throw new ForbiddenException();
        }
    }

    public async Task EnsureCanMarkPaymentAsync(Guid planId, Guid partnerId, CancellationToken cancellationToken = default)
    {
        await EnsureMemberAsync(planId, cancellationToken);
        var userId = RequireUserId();

        if (_currentUser.IsSuperAdmin || await IsOwnerAsync(planId, cancellationToken))
        {
            var exists = await _db.Partners.AsNoTracking()
                .AnyAsync(p => p.Id == partnerId && p.PlanId == planId, cancellationToken);
            if (!exists)
            {
                throw new NotFoundException(nameof(Partner), partnerId);
            }

            return;
        }

        var partner = await _db.Partners.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == partnerId && p.PlanId == planId, cancellationToken)
            ?? throw new NotFoundException(nameof(Partner), partnerId);

        if (string.Equals(partner.LinkedUserId, userId, StringComparison.Ordinal))
        {
            return;
        }

        var memberLink = await _db.PlanMembers.AsNoTracking()
            .AnyAsync(m => m.PlanId == planId && m.UserId == userId && m.PartnerId == partnerId, cancellationToken);

        if (!memberLink)
        {
            throw new ForbiddenException("Sadece kendi ortağınızın ödemesini işaretleyebilirsiniz.");
        }
    }

    public async Task<bool> IsOwnerAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        if (_currentUser.IsSuperAdmin)
        {
            return true;
        }

        var userId = _currentUser.UserId;
        if (userId is null)
        {
            return false;
        }

        return await _db.Plans.AsNoTracking()
            .AnyAsync(p => p.Id == planId && p.OwnerUserId == userId, cancellationToken);
    }

    public async Task<Guid?> GetMyPartnerIdAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
        {
            return null;
        }

        var fromPartner = await _db.Partners.AsNoTracking()
            .Where(p => p.PlanId == planId && p.LinkedUserId == userId)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (fromPartner is not null)
        {
            return fromPartner;
        }

        return await _db.PlanMembers.AsNoTracking()
            .Where(m => m.PlanId == planId && m.UserId == userId)
            .Select(m => m.PartnerId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private string RequireUserId() =>
        _currentUser.UserId ?? throw new ForbiddenException();
}
