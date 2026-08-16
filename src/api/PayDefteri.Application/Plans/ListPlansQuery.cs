using PayDefteri.Application.Common.Interfaces;
using PayDefteri.Application.Common.Mapping;
using PayDefteri.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace PayDefteri.Application.Plans;

public sealed record ListPlansQuery(bool IncludeArchived = false) : IRequest<IReadOnlyList<PlanDto>>;

public sealed class ListPlansQueryHandler : IRequestHandler<ListPlansQuery, IReadOnlyList<PlanDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListPlansQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<PlanDto>> Handle(ListPlansQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var query = _db.Plans
            .AsNoTracking()
            .Where(p => p.OwnerUserId == userId
                        || _db.PlanMembers.Any(m => m.PlanId == p.Id && m.UserId == userId));

        if (request.IncludeArchived)
        {
            query = query.Where(p => p.IsDeleted && p.OwnerUserId == userId);
        }
        else
        {
            query = query.Where(p => !p.IsDeleted);
        }

        return await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => new PlanDto(
                p.Id,
                p.PlanType,
                p.Title,
                p.Description,
                p.DeliveryInstallmentId,
                p.CreatedAtUtc,
                p.RequireReceipt,
                p.IbanMode,
                p.SettlementIban,
                p.RemindersEnabled,
                p.ReminderDaysBefore,
                p.ReminderDaysAfter,
                p.IsDeleted))
            .ToListAsync(cancellationToken);
    }
}
