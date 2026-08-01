using FuzulTaksitTakip.Application.Common.Interfaces;
using FuzulTaksitTakip.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FuzulTaksitTakip.Application.Plans;

public sealed record ListPlanActivityQuery(Guid PlanId) : IRequest<IReadOnlyList<PlanActivityItemDto>>;

public sealed class ListPlanActivityQueryHandler
    : IRequestHandler<ListPlanActivityQuery, IReadOnlyList<PlanActivityItemDto>>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;

    public ListPlanActivityQueryHandler(IAppDbContext db, IPlanAuthorization auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task<IReadOnlyList<PlanActivityItemDto>> Handle(
        ListPlanActivityQuery request,
        CancellationToken cancellationToken)
    {
        await _auth.EnsureMemberAsync(request.PlanId, cancellationToken);

        return await _db.PlanActivityLogs.AsNoTracking()
            .Where(a => a.PlanId == request.PlanId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(100)
            .Select(a => new PlanActivityItemDto(
                a.Id,
                a.Type.ToString(),
                a.Message,
                a.ActorDisplayName,
                a.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
