using FuzulTaksitTakip.Application.Common.Exceptions;
using FuzulTaksitTakip.Application.Common.Interfaces;
using FuzulTaksitTakip.Application.Common.Models;
using FuzulTaksitTakip.Domain.Entities;
using FuzulTaksitTakip.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FuzulTaksitTakip.Application.Plans;

public sealed record GetReportSummaryQuery(Guid PlanId) : IRequest<ReportSummaryDto>;

public sealed class GetReportSummaryQueryHandler : IRequestHandler<GetReportSummaryQuery, ReportSummaryDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;

    public GetReportSummaryQueryHandler(IAppDbContext db, IPlanAuthorization auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task<ReportSummaryDto> Handle(GetReportSummaryQuery request, CancellationToken cancellationToken)
    {
        await _auth.EnsureMemberAsync(request.PlanId, cancellationToken);

        _ = await _db.Plans.AsNoTracking()
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
            .OrderBy(i => i.DueDate)
            .ToListAsync(cancellationToken);

        decimal grandTotal = 0m;
        decimal grandPaid = 0m;
        var totals = partners.ToDictionary(p => p.Id, _ => 0m);
        var paidAmounts = partners.ToDictionary(p => p.Id, _ => 0m);
        var monthBuckets = new Dictionary<string, (decimal Total, decimal Paid, int Count)>();

        foreach (var inst in installments)
        {
            grandTotal += inst.TotalAmount;
            var key = $"{inst.DueDate.Year:D4}-{inst.DueDate.Month:D2}";
            if (!monthBuckets.TryGetValue(key, out var bucket))
            {
                bucket = (0m, 0m, 0);
            }

            bucket.Total += inst.TotalAmount;
            bucket.Count += 1;

            decimal paidOnInst = 0m;
            foreach (var partner in partners)
            {
                var share = ShareCalculator.GetPartnerShare(inst, partner, partners);
                totals[partner.Id] += share;
                var payment = inst.Payments.FirstOrDefault(p => p.PartnerId == partner.Id);
                if (payment?.IsPaid == true)
                {
                    paidAmounts[partner.Id] += share;
                    grandPaid += share;
                    paidOnInst += share;
                }
            }

            bucket.Paid += paidOnInst;
            monthBuckets[key] = bucket;
        }

        var bars = partners.Select(p => new ReportPartnerBarDto(
            p.Id,
            p.Name,
            p.Color,
            paidAmounts[p.Id],
            totals[p.Id] - paidAmounts[p.Id],
            totals[p.Id])).ToList();

        var months = monthBuckets
            .OrderBy(kv => kv.Key)
            .Select(kv => new ReportMonthDto(
                kv.Key,
                kv.Value.Total,
                kv.Value.Paid,
                kv.Value.Total - kv.Value.Paid,
                kv.Value.Count))
            .ToList();

        var remaining = grandTotal - grandPaid;
        var pct = grandTotal > 0 ? Math.Round(grandPaid / grandTotal * 100m, 1) : 0m;

        return new ReportSummaryDto(
            bars,
            months,
            new DashboardMetricsDto(grandTotal, grandPaid, remaining, pct));
    }
}
