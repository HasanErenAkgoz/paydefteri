using FuzulTaksitTakip.Application.Common.Exceptions;
using FuzulTaksitTakip.Application.Common.Interfaces;
using FuzulTaksitTakip.Application.Common.Models;
using FuzulTaksitTakip.Domain.Entities;
using FuzulTaksitTakip.Domain.Enums;
using FuzulTaksitTakip.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FuzulTaksitTakip.Application.Plans;

public sealed record GetDashboardQuery(Guid PlanId) : IRequest<DashboardDto>;

public sealed class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;

    public GetDashboardQueryHandler(IAppDbContext db, IPlanAuthorization auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        await _auth.EnsureMemberAsync(request.PlanId, cancellationToken);
        var myPartnerId = await _auth.GetMyPartnerIdAsync(request.PlanId, cancellationToken);
        var isOwner = await _auth.IsOwnerAsync(request.PlanId, cancellationToken);

        var plan = await _db.Plans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PlanId && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Plan), request.PlanId);

        var partners = await _db.Partners.AsNoTracking()
            .Where(p => p.PlanId == request.PlanId)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(cancellationToken);

        var installments = await _db.Installments.AsNoTracking()
            .Include(i => i.CustomShares)
            .Include(i => i.Payments)
            .Where(i => i.PlanId == request.PlanId)
            .OrderBy(i => i.SortOrder)
            .ToListAsync(cancellationToken);

        var partnersCount = partners.Count;
        decimal grandTotal = 0m;
        decimal grandPaid = 0m;
        var pendingApprovalCount = 0;

        var totals = partners.ToDictionary(p => p.Id, _ => 0m);
        var paidAmounts = partners.ToDictionary(p => p.Id, _ => 0m);

        var dashboardInstallments = new List<DashboardInstallmentDto>();

        foreach (var inst in installments)
        {
            grandTotal += inst.TotalAmount;
            var status = InstallmentStatusCalculator.Calculate(inst, partnersCount);
            var partnerPayments = new List<PartnerPaymentStatusDto>();

            foreach (var partner in partners)
            {
                var share = ShareCalculator.GetPartnerShare(inst, partner, partners);
                totals[partner.Id] += share;

                var payment = inst.Payments.FirstOrDefault(p => p.PartnerId == partner.Id);
                var isPaid = payment?.IsPaid == true;
                var review = payment?.ReviewStatus ?? PaymentReviewStatus.None;
                if (review == PaymentReviewStatus.Pending)
                {
                    pendingApprovalCount++;
                }

                if (isPaid)
                {
                    paidAmounts[partner.Id] += share;
                    grandPaid += share;
                }

                partnerPayments.Add(new PartnerPaymentStatusDto(
                    partner.Id,
                    partner.Name,
                    share,
                    isPaid,
                    payment?.PaidAt,
                    payment?.PaidByPartnerId,
                    payment?.Note ?? string.Empty,
                    !string.IsNullOrEmpty(payment?.ReceiptStorageKey),
                    review));
            }

            dashboardInstallments.Add(new DashboardInstallmentDto(
                inst.Id,
                inst.Name,
                inst.DueDate,
                inst.TotalAmount,
                inst.ShareType,
                status,
                inst.SortOrder,
                partnerPayments));
        }

        var partnerSummaries = partners.Select(p => new PartnerSummaryDto(
            p.Id,
            p.Name,
            p.Color,
            totals[p.Id],
            paidAmounts[p.Id],
            totals[p.Id] - paidAmounts[p.Id],
            p.Iban)).ToList();

        var balances = SettlementCalculator.ComputeBalances(installments, partners);
        var settlements = partners.Select(p => new SettlementBalanceDto(
            p.Id,
            p.Name,
            balances.GetValueOrDefault(p.Id))).ToList();

        int? daysUntilDelivery = null;
        if (plan.DeliveryInstallmentId is Guid deliveryId)
        {
            var delivery = installments.FirstOrDefault(i => i.Id == deliveryId);
            if (delivery is not null)
            {
                daysUntilDelivery = delivery.DueDate.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber;
            }
        }

        string? paymentTargetIban = plan.IbanMode switch
        {
            IbanMode.Plan => plan.SettlementIban,
            IbanMode.Partner when myPartnerId is Guid mine =>
                partners.FirstOrDefault(p => p.Id == mine)?.Iban,
            _ => null
        };

        var remaining = grandTotal - grandPaid;
        var pct = grandTotal > 0 ? Math.Round(grandPaid / grandTotal * 100m, 1) : 0m;

        MyShareMetricsDto? myMetrics = null;
        if (myPartnerId is Guid mid)
        {
            var unpaid = dashboardInstallments
                .Select(i => (Inst: i, Pay: i.PartnerPayments.FirstOrDefault(p => p.PartnerId == mid)))
                .Where(x => x.Pay is not null && !x.Pay.IsPaid)
                .OrderBy(x => x.Inst.DueDate)
                .ToList();
            var next = unpaid.FirstOrDefault();
            myMetrics = new MyShareMetricsDto(
                totals[mid] - paidAmounts[mid],
                paidAmounts[mid],
                totals[mid],
                unpaid.Count,
                next.Inst?.DueDate,
                next.Inst?.Name);
        }

        return new DashboardDto(
            plan.Id,
            plan.Title,
            plan.Description,
            plan.DeliveryInstallmentId,
            daysUntilDelivery,
            myPartnerId,
            isOwner,
            plan.RequireReceipt,
            plan.IbanMode,
            plan.SettlementIban,
            paymentTargetIban,
            new DashboardMetricsDto(grandTotal, grandPaid, remaining, pct),
            partnerSummaries,
            settlements,
            dashboardInstallments,
            myMetrics,
            pendingApprovalCount);
    }
}
