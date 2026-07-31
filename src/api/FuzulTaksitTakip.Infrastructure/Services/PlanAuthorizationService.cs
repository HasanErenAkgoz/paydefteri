using FuzulTaksitTakip.Application.Common.Exceptions;
using FuzulTaksitTakip.Application.Common.Interfaces;
using FuzulTaksitTakip.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FuzulTaksitTakip.Infrastructure.Services;

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
        var userId = _currentUser.UserId
            ?? throw new ForbiddenException();

        var ownerId = await _db.Plans
            .AsNoTracking()
            .Where(p => p.Id == planId)
            .Select(p => (string?)p.OwnerUserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (ownerId is null)
        {
            throw new NotFoundException(nameof(Plan), planId);
        }

        if (!string.Equals(ownerId, userId, StringComparison.Ordinal))
        {
            throw new ForbiddenException();
        }
    }

    public Task EnsureOwnerAsync(Plan plan, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new ForbiddenException();

        if (!string.Equals(plan.OwnerUserId, userId, StringComparison.Ordinal))
        {
            throw new ForbiddenException();
        }

        return Task.CompletedTask;
    }
}
