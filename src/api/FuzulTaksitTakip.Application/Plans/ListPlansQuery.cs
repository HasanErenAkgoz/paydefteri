using FuzulTaksitTakip.Application.Common.Interfaces;
using FuzulTaksitTakip.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FuzulTaksitTakip.Application.Plans;

public sealed record ListPlansQuery : IRequest<IReadOnlyList<PlanDto>>;

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

        return await _db.Plans
            .AsNoTracking()
            .Where(p => p.OwnerUserId == userId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => new PlanDto(p.Id, p.Title, p.Description, p.DeliveryInstallmentId, p.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
