using FuzulTaksitTakip.Application.Common.Exceptions;
using FuzulTaksitTakip.Application.Common.Interfaces;
using FuzulTaksitTakip.Application.Common.Models;
using FuzulTaksitTakip.Domain.Entities;
using FuzulTaksitTakip.Domain.Templates;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FuzulTaksitTakip.Application.Plans;

public sealed record SeedFuzulCommand(Guid PlanId) : IRequest<PlanDto>;

public sealed class SeedFuzulCommandHandler : IRequestHandler<SeedFuzulCommand, PlanDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;
    private readonly ICurrentUser _currentUser;

    public SeedFuzulCommandHandler(IAppDbContext db, IPlanAuthorization auth, ICurrentUser currentUser)
    {
        _db = db;
        _auth = auth;
        _currentUser = currentUser;
    }

    public async Task<PlanDto> Handle(SeedFuzulCommand request, CancellationToken cancellationToken)
    {
        await _auth.EnsureOwnerAsync(request.PlanId, cancellationToken);

        var plan = await _db.Plans
            .Include(p => p.Partners)
            .Include(p => p.Installments)
                .ThenInclude(i => i.CustomShares)
            .Include(p => p.Installments)
                .ThenInclude(i => i.Payments)
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken)
            ?? throw new NotFoundException(nameof(Plan), request.PlanId);

        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        // Clear children (payments/shares via cascade or explicit)
        foreach (var inst in plan.Installments.ToList())
        {
            _db.Payments.RemoveRange(inst.Payments);
            _db.InstallmentShares.RemoveRange(inst.CustomShares);
            _db.Installments.Remove(inst);
        }

        _db.Partners.RemoveRange(plan.Partners.ToList());
        await _db.SaveChangesAsync(cancellationToken);

        var (seedPlan, partners, installments) = FuzulSeedTemplate.Create(userId);

        plan.Title = seedPlan.Title;
        plan.Description = seedPlan.Description;
        plan.DeliveryInstallmentId = null;
        plan.UpdatedAtUtc = DateTime.UtcNow;

        // Remap seed entities onto existing plan id
        Guid? deliveryId = null;
        var deliverySeedId = seedPlan.DeliveryInstallmentId;

        foreach (var partner in partners)
        {
            partner.PlanId = plan.Id;
            _db.Partners.Add(partner);
        }

        foreach (var inst in installments)
        {
            inst.PlanId = plan.Id;
            if (deliverySeedId == inst.Id)
            {
                deliveryId = inst.Id;
            }

            _db.Installments.Add(inst);
        }

        await _db.SaveChangesAsync(cancellationToken);

        plan.DeliveryInstallmentId = deliveryId;
        await _db.SaveChangesAsync(cancellationToken);

        return new PlanDto(plan.Id, plan.Title, plan.Description, plan.DeliveryInstallmentId, plan.CreatedAtUtc);
    }
}
