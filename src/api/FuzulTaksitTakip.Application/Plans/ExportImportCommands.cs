using System.Text.Json;
using FluentValidation;
using FuzulTaksitTakip.Application.Common;
using FuzulTaksitTakip.Application.Common.Exceptions;
using FuzulTaksitTakip.Application.Common.Interfaces;
using FuzulTaksitTakip.Application.Common.Mapping;
using FuzulTaksitTakip.Application.Common.Models;
using FuzulTaksitTakip.Domain.Entities;
using FuzulTaksitTakip.Domain.Enums;
using FuzulTaksitTakip.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FuzulTaksitTakip.Application.Plans;

public sealed record ExportPlanQuery(Guid PlanId) : IRequest<PlanExportDto>;

public sealed class ExportPlanQueryHandler : IRequestHandler<ExportPlanQuery, PlanExportDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;

    public ExportPlanQueryHandler(IAppDbContext db, IPlanAuthorization auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task<PlanExportDto> Handle(ExportPlanQuery request, CancellationToken cancellationToken)
    {
        await _auth.EnsureMemberAsync(request.PlanId, cancellationToken);

        var plan = await _db.Plans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken)
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

        return new PlanExportDto(
            plan.Title,
            plan.Description,
            plan.DeliveryInstallmentId,
            partners.Select(p => new PartnerExportDto(p.Id, p.Name, p.Color, p.DefaultPct, p.SortOrder)).ToList(),
            installments.Select(i => new InstallmentExportDto(
                i.Id,
                i.Name,
                i.DueDate,
                i.TotalAmount,
                i.ShareType.ToString(),
                i.SortOrder,
                i.CustomShares.Select(s => new CustomShareDto(s.PartnerId, s.Amount)).ToList(),
                i.Payments.Select(p => new PaymentDto(
                    p.PartnerId, p.IsPaid, p.PaidAt, p.PaidByPartnerId, p.Note,
                    !string.IsNullOrEmpty(p.ReceiptStorageKey), p.ReviewStatus)).ToList()
            )).ToList());
    }
}

public sealed record ImportPlanCommand(Guid PlanId, PlanExportDto Data) : IRequest<PlanDto>;

public sealed class ImportPlanCommandValidator : AbstractValidator<ImportPlanCommand>
{
    public const int MaxPartners = 50;
    public const int MaxInstallments = 500;
    public const int MaxJsonChars = 1_000_000;

    public ImportPlanCommandValidator()
    {
        RuleFor(x => x.Data).NotNull();
        RuleFor(x => x.Data.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Data.Description).MaximumLength(2000);
        RuleFor(x => x.Data.Partners).NotNull().Must(p => p.Count is > 0 and <= MaxPartners)
            .WithMessage($"Partners count must be between 1 and {MaxPartners}.");
        RuleFor(x => x.Data.Installments).NotNull().Must(i => i.Count <= MaxInstallments)
            .WithMessage($"At most {MaxInstallments} installments allowed.");
        RuleFor(x => x.Data).Must(d => JsonSerializer.Serialize(d).Length <= MaxJsonChars)
            .WithMessage("Import payload exceeds size limit.");

        RuleForEach(x => x.Data.Partners).ChildRules(p =>
        {
            p.RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            p.RuleFor(x => x.Color).NotEmpty().MaximumLength(32);
            p.RuleFor(x => x.DefaultPct).InclusiveBetween(0m, 100m);
        });

        RuleForEach(x => x.Data.Installments).ChildRules(i =>
        {
            i.RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
            i.RuleFor(x => x.TotalAmount).GreaterThanOrEqualTo(0m);
            i.RuleFor(x => x.ShareType).NotEmpty();
        });
    }
}

public sealed class ImportPlanCommandHandler : IRequestHandler<ImportPlanCommand, PlanDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;
    private readonly ICurrentUser _currentUser;

    public ImportPlanCommandHandler(IAppDbContext db, IPlanAuthorization auth, ICurrentUser currentUser)
    {
        _db = db;
        _auth = auth;
        _currentUser = currentUser;
    }

    public async Task<PlanDto> Handle(ImportPlanCommand request, CancellationToken cancellationToken)
    {
        await _auth.EnsureOwnerAsync(request.PlanId, cancellationToken);

        var data = request.Data;

        var partnerEntries = data.Partners
            .Select(p => (Export: p, Id: p.Id == Guid.Empty ? Guid.NewGuid() : p.Id))
            .ToList();

        if (partnerEntries.Select(p => p.Id).Distinct().Count() != partnerEntries.Count)
        {
            throw new ValidationException("Duplicate partner ids in import.");
        }

        if (!ShareCalculator.DefaultPercentagesSumTo100(
                partnerEntries.Select(p => new Partner { Id = p.Id, DefaultPct = p.Export.DefaultPct })))
        {
            throw new ValidationException("Partner defaultPct values must sum to 100.");
        }

        var partnerIdSet = partnerEntries.Select(p => p.Id).ToHashSet();

        var installmentEntries = data.Installments
            .Select(i => (Export: i, Id: i.Id == Guid.Empty ? Guid.NewGuid() : i.Id))
            .ToList();

        if (installmentEntries.Select(i => i.Id).Distinct().Count() != installmentEntries.Count)
        {
            throw new ValidationException("Duplicate installment ids in import.");
        }

        foreach (var (export, _) in installmentEntries)
        {
            if (!Enum.TryParse<ShareType>(export.ShareType, ignoreCase: true, out var shareType))
            {
                throw new ValidationException($"Unknown shareType '{export.ShareType}'.");
            }

            if (shareType == ShareType.Custom)
            {
                var probe = new Installment
                {
                    TotalAmount = export.TotalAmount,
                    ShareType = ShareType.Custom,
                    CustomShares = export.CustomShares
                        .Select(s => new InstallmentShare { PartnerId = s.PartnerId, Amount = s.Amount })
                        .ToList()
                };
                if (!ShareCalculator.CustomSharesMatchTotal(probe))
                {
                    throw new ValidationException($"Custom shares for '{export.Name}' must equal totalAmount.");
                }
            }

            foreach (var share in export.CustomShares)
            {
                if (!partnerIdSet.Contains(share.PartnerId))
                {
                    throw new ValidationException($"customShare partnerId {share.PartnerId} is unknown.");
                }
            }

            foreach (var payment in export.Payments)
            {
                if (!partnerIdSet.Contains(payment.PartnerId))
                {
                    throw new ValidationException($"payment partnerId {payment.PartnerId} is unknown.");
                }

                if (payment.PaidByPartnerId is Guid paidBy && !partnerIdSet.Contains(paidBy))
                {
                    throw new ValidationException($"paidByPartnerId {paidBy} is unknown.");
                }
            }
        }

        return await _db.ExecuteInTransactionAsync(async transactionCt =>
        {
        var plan = await _db.Plans
            .Include(p => p.Partners)
            .Include(p => p.Installments).ThenInclude(i => i.CustomShares)
            .Include(p => p.Installments).ThenInclude(i => i.Payments)
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, transactionCt)
            ?? throw new NotFoundException(nameof(Plan), request.PlanId);

        foreach (var inst in plan.Installments.ToList())
        {
            _db.Payments.RemoveRange(inst.Payments);
            _db.InstallmentShares.RemoveRange(inst.CustomShares);
            _db.Installments.Remove(inst);
        }

        _db.Partners.RemoveRange(plan.Partners.ToList());
        await _db.SaveChangesAsync(transactionCt);

        plan.Title = data.Title.Trim();
        plan.Description = data.Description?.Trim() ?? string.Empty;
        plan.DeliveryInstallmentId = null;
        plan.UpdatedAtUtc = DateTime.UtcNow;

        foreach (var (export, id) in partnerEntries)
        {
            _db.Partners.Add(new Partner
            {
                Id = id,
                PlanId = plan.Id,
                Name = export.Name.Trim(),
                Color = export.Color,
                DefaultPct = export.DefaultPct,
                SortOrder = export.SortOrder
            });
        }

        Guid? deliveryId = null;
        if (data.DeliveryInstallmentId is Guid deliveryExportId)
        {
            deliveryId = installmentEntries
                .Where(i => i.Export.Id == deliveryExportId || i.Id == deliveryExportId)
                .Select(i => (Guid?)i.Id)
                .FirstOrDefault();
        }

        foreach (var (export, instId) in installmentEntries)
        {
            Enum.TryParse<ShareType>(export.ShareType, ignoreCase: true, out var shareType);

            var installment = new Installment
            {
                Id = instId,
                PlanId = plan.Id,
                Name = export.Name.Trim(),
                DueDate = export.DueDate,
                TotalAmount = export.TotalAmount,
                ShareType = shareType,
                SortOrder = export.SortOrder
            };

            foreach (var share in export.CustomShares)
            {
                installment.CustomShares.Add(new InstallmentShare
                {
                    InstallmentId = instId,
                    PartnerId = share.PartnerId,
                    Amount = share.Amount
                });
            }

            foreach (var payment in export.Payments)
            {
                installment.Payments.Add(new Payment
                {
                    InstallmentId = instId,
                    PartnerId = payment.PartnerId,
                    IsPaid = payment.IsPaid,
                    PaidAt = payment.PaidAt,
                    PaidByPartnerId = payment.PaidByPartnerId,
                    Note = payment.Note ?? string.Empty
                });
            }

            _db.Installments.Add(installment);
        }

        await _db.SaveChangesAsync(transactionCt);

        plan.DeliveryInstallmentId = deliveryId;
        PlanActivity.Write(_db, _currentUser, plan.Id, PlanActivityType.PlanImported, $"Plan içe aktarıldı: {plan.Title}");
        await _db.SaveChangesAsync(transactionCt);

        return plan.ToDto();
        }, cancellationToken);
    }
}
