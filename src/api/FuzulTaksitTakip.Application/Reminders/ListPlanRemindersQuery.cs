using FuzulTaksitTakip.Application.Common.Interfaces;
using FuzulTaksitTakip.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FuzulTaksitTakip.Application.Reminders;

public sealed record ListPlanRemindersQuery(Guid PlanId) : IRequest<IReadOnlyList<ReminderHistoryItemDto>>;

public sealed class ListPlanRemindersQueryHandler
    : IRequestHandler<ListPlanRemindersQuery, IReadOnlyList<ReminderHistoryItemDto>>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;

    public ListPlanRemindersQueryHandler(IAppDbContext db, IPlanAuthorization auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task<IReadOnlyList<ReminderHistoryItemDto>> Handle(
        ListPlanRemindersQuery request,
        CancellationToken cancellationToken)
    {
        await _auth.EnsureMemberAsync(request.PlanId, cancellationToken);

        var logs = await _db.PaymentReminderLogs.AsNoTracking()
            .Where(l => l.PlanId == request.PlanId)
            .OrderByDescending(l => l.SentOn)
            .ThenByDescending(l => l.CreatedAtUtc)
            .Take(200)
            .ToListAsync(cancellationToken);

        var installmentIds = logs.Select(l => l.InstallmentId).Distinct().ToList();
        var partnerIds = logs.Where(l => l.PartnerId is not null).Select(l => l.PartnerId!.Value).Distinct().ToList();

        var installmentNames = await _db.Installments.AsNoTracking()
            .Where(i => installmentIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i.Name, cancellationToken);

        var partnerNames = await _db.Partners.AsNoTracking()
            .Where(p => partnerIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        return logs.Select(l => new ReminderHistoryItemDto(
            l.Id,
            l.InstallmentId,
            installmentNames.GetValueOrDefault(l.InstallmentId, "Taksit"),
            l.PartnerId,
            l.PartnerId is Guid pid ? partnerNames.GetValueOrDefault(pid) : "Plan sahibi",
            l.Kind.ToString(),
            l.OffsetDays,
            l.SentOn,
            l.CreatedAtUtc)).ToList();
    }
}
