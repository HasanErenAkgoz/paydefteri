using FuzulTaksitTakip.Application.Common;
using FuzulTaksitTakip.Application.Common.Exceptions;
using FuzulTaksitTakip.Application.Common.Interfaces;
using FuzulTaksitTakip.Application.Common.Mapping;
using FuzulTaksitTakip.Application.Common.Models;
using FuzulTaksitTakip.Domain.Entities;
using FuzulTaksitTakip.Domain.Templates;
using MediatR;
using Microsoft.EntityFrameworkCore;
using FuzulTaksitTakip.Domain.Enums;

namespace FuzulTaksitTakip.Application.Plans;

public sealed record SeedPlanTemplateCommand(Guid PlanId, string TemplateKey) : IRequest<PlanDto>;

public sealed class SeedPlanTemplateCommandHandler : IRequestHandler<SeedPlanTemplateCommand, PlanDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;
    private readonly ICurrentUser _currentUser;

    public SeedPlanTemplateCommandHandler(IAppDbContext db, IPlanAuthorization auth, ICurrentUser currentUser)
    {
        _db = db;
        _auth = auth;
        _currentUser = currentUser;
    }

    public async Task<PlanDto> Handle(SeedPlanTemplateCommand request, CancellationToken cancellationToken)
    {
        await _auth.EnsureOwnerAsync(request.PlanId, cancellationToken);

        if (!PlanTemplateCatalog.TryGet(request.TemplateKey, out var def) || def is null)
        {
            throw new NotFoundException("Template", request.TemplateKey);
        }

        var plan = await _db.Plans
            .Include(p => p.Partners)
            .Include(p => p.Installments)
                .ThenInclude(i => i.CustomShares)
            .Include(p => p.Installments)
                .ThenInclude(i => i.Payments)
            .FirstOrDefaultAsync(p => p.Id == request.PlanId && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Plan), request.PlanId);

        var members = await _db.PlanMembers
            .Where(m => m.PlanId == request.PlanId)
            .ToListAsync(cancellationToken);
        foreach (var m in members)
        {
            m.PartnerId = null;
            m.UpdatedAtUtc = DateTime.UtcNow;
        }

        foreach (var inst in plan.Installments.ToList())
        {
            _db.Payments.RemoveRange(inst.Payments);
            _db.InstallmentShares.RemoveRange(inst.CustomShares);
            _db.Installments.Remove(inst);
        }

        _db.Partners.RemoveRange(plan.Partners.ToList());
        await _db.SaveChangesAsync(cancellationToken);

        plan.Title = def.Title;
        plan.Description = def.Description;
        plan.DeliveryInstallmentId = null;
        plan.UpdatedAtUtc = DateTime.UtcNow;

        var (partners, installments, deliveryId) = PlanTemplateCatalog.Materialize(def, plan.Id);
        foreach (var partner in partners)
        {
            _db.Partners.Add(partner);
        }

        foreach (var inst in installments)
        {
            _db.Installments.Add(inst);
        }

        await _db.SaveChangesAsync(cancellationToken);

        plan.DeliveryInstallmentId = deliveryId;
        PlanActivity.Write(_db, _currentUser, plan.Id, PlanActivityType.PlanSeeded, $"Şablon yüklendi: {def.Title}");
        await _db.SaveChangesAsync(cancellationToken);

        return plan.ToDto();
    }
}

public sealed record SeedFuzulCommand(Guid PlanId) : IRequest<PlanDto>;

public sealed class SeedFuzulCommandHandler : IRequestHandler<SeedFuzulCommand, PlanDto>
{
    private readonly ISender _sender;

    public SeedFuzulCommandHandler(ISender sender) => _sender = sender;

    public Task<PlanDto> Handle(SeedFuzulCommand request, CancellationToken cancellationToken)
        => _sender.Send(new SeedPlanTemplateCommand(request.PlanId, "fuzul"), cancellationToken);
}

public sealed record SettleUpCommand(Guid PlanId) : IRequest;

public sealed class SettleUpCommandHandler : IRequestHandler<SettleUpCommand>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;
    private readonly ICurrentUser _currentUser;

    public SettleUpCommandHandler(IAppDbContext db, IPlanAuthorization auth, ICurrentUser currentUser)
    {
        _db = db;
        _auth = auth;
        _currentUser = currentUser;
    }

    public async Task Handle(SettleUpCommand request, CancellationToken cancellationToken)
    {
        await _auth.EnsureOwnerAsync(request.PlanId, cancellationToken);

        var payments = await _db.Payments
            .Include(p => p.Installment)
            .Where(p => p.Installment.PlanId == request.PlanId && p.IsPaid)
            .ToListAsync(cancellationToken);

        foreach (var payment in payments)
        {
            payment.PaidByPartnerId = payment.PartnerId;
            payment.UpdatedAtUtc = DateTime.UtcNow;
        }

        PlanActivity.Write(
            _db,
            _currentUser,
            request.PlanId,
            PlanActivityType.SettleUp,
            $"Hesaplaşma uygulandı ({payments.Count} ödeme satırı güncellendi)");

        await _db.SaveChangesAsync(cancellationToken);
    }
}

public sealed record GetTemplatePreviewQuery(string TemplateKey) : IRequest<PlanTemplatePreviewDto>;

public sealed record TemplateInstallmentPreviewDto(
    int Index,
    string Name,
    DateOnly DueDate,
    decimal TotalAmount,
    decimal PerPartnerAmount);

public sealed record TemplatePartnerPreviewDto(
    string Name,
    string Color,
    decimal DefaultPct);

public sealed record PlanTemplatePreviewDto(
    string Key,
    string Title,
    string Description,
    decimal GrandTotal,
    int InstallmentCount,
    string? DeliveryName,
    int DeliveryIndex,
    int PartnerCount,
    IReadOnlyList<TemplatePartnerPreviewDto> Partners,
    IReadOnlyList<TemplateInstallmentPreviewDto> Installments);

public sealed class GetTemplatePreviewQueryHandler : IRequestHandler<GetTemplatePreviewQuery, PlanTemplatePreviewDto>
{
    public Task<PlanTemplatePreviewDto> Handle(GetTemplatePreviewQuery request, CancellationToken cancellationToken)
    {
        if (!PlanTemplateCatalog.TryGet(request.TemplateKey, out var def) || def is null)
        {
            throw new NotFoundException("Template", request.TemplateKey);
        }

        var preview = PlanTemplateCatalog.ToPreview(def);
        var partnerCount = Math.Max(preview.PartnerCount, 1);
        var partners = def.Partners
            .Select(p => new TemplatePartnerPreviewDto(p.Name, p.Color, p.DefaultPct))
            .ToList();
        var rows = preview.Installments
            .Select((inst, idx) => new TemplateInstallmentPreviewDto(
                idx + 1,
                inst.Name,
                inst.DueDate,
                inst.TotalAmount,
                Math.Round(inst.TotalAmount / partnerCount, 2)))
            .ToList();

        return Task.FromResult(new PlanTemplatePreviewDto(
            preview.Key,
            preview.Title,
            preview.Description,
            preview.GrandTotal,
            preview.InstallmentCount,
            preview.DeliveryName,
            def.DeliveryIndex,
            preview.PartnerCount,
            partners,
            rows));
    }
}

public sealed record ListTemplateKeysQuery : IRequest<IReadOnlyList<TemplateListItemDto>>;

public sealed record TemplateListItemDto(string Key, string Title, string Description);

public sealed class ListTemplateKeysQueryHandler : IRequestHandler<ListTemplateKeysQuery, IReadOnlyList<TemplateListItemDto>>
{
    public Task<IReadOnlyList<TemplateListItemDto>> Handle(ListTemplateKeysQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<TemplateListItemDto> list = PlanTemplateCatalog.Keys
            .Select(k =>
            {
                var def = PlanTemplateCatalog.Get(k);
                return new TemplateListItemDto(def.Key, def.Title, def.Description);
            })
            .ToList();
        return Task.FromResult(list);
    }
}
