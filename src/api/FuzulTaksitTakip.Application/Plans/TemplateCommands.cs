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

public sealed record SeedTemplatePartnerBody(string Name, string Color, decimal DefaultPct);

public sealed record SeedTemplateExpenseBody(string Name, DateOnly OccurredOn, decimal TotalAmount);

public sealed record SeedTemplateBody(
    string? Title,
    string? Description,
    IReadOnlyList<SeedTemplatePartnerBody>? Partners,
    IReadOnlyList<SeedTemplateExpenseBody>? Expenses = null);

public sealed record SeedPlanTemplateCommand(
    Guid PlanId,
    string TemplateKey,
    SeedTemplateBody? Body = null) : IRequest<PlanDto>;

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

        if (ExpensePlanTemplateCatalog.IsExpenseTemplate(request.TemplateKey))
        {
            return await SeedExpenseTemplateAsync(request.PlanId, request.TemplateKey, request.Body, cancellationToken);
        }

        if (!PlanTemplateCatalog.TryGet(request.TemplateKey, out var def) || def is null)
        {
            throw new NotFoundException("Template", request.TemplateKey);
        }

        var plan = await _db.Plans
            .FirstOrDefaultAsync(p => p.Id == request.PlanId && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Plan), request.PlanId);

        if (plan.PlanType == PlanType.Expense)
        {
            throw new InvalidOperationException("Bu şablon yalnızca taksit planlarına yüklenebilir.");
        }

        var members = await _db.PlanMembers
            .Where(m => m.PlanId == request.PlanId)
            .ToListAsync(cancellationToken);
        foreach (var m in members)
        {
            m.PartnerId = null;
            m.UpdatedAtUtc = DateTime.UtcNow;
        }

        var existingInstallments = await _db.Installments
            .Where(i => i.PlanId == plan.Id && !i.IsDeleted)
            .ToListAsync(cancellationToken);
        var installmentIds = existingInstallments.Select(i => i.Id).ToArray();
        var payments = await _db.Payments
            .Where(p => installmentIds.Contains(p.InstallmentId))
            .ToListAsync(cancellationToken);
        var customShares = await _db.InstallmentShares
            .Where(s => installmentIds.Contains(s.InstallmentId))
            .ToListAsync(cancellationToken);
        var partnersToRemove = await _db.Partners
            .Where(p => p.PlanId == plan.Id && !p.IsDeleted)
            .ToListAsync(cancellationToken);

        _db.Payments.RemoveRange(payments);
        _db.InstallmentShares.RemoveRange(customShares);
        _db.Installments.RemoveRange(existingInstallments);
        _db.Partners.RemoveRange(partnersToRemove);
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

    private async Task<PlanDto> SeedExpenseTemplateAsync(
        Guid planId,
        string templateKey,
        SeedTemplateBody? body,
        CancellationToken cancellationToken)
    {
        if (!ExpensePlanTemplateCatalog.TryGetMeta(templateKey, out var meta) || meta is null)
        {
            throw new NotFoundException("Template", templateKey);
        }

        ExpenseCoupleSeedOptions? options = null;
        if (body is not null)
        {
            IReadOnlyList<ExpenseCouplePartnerSeed>? partnerSeeds = null;
            if (body.Partners is { Count: > 0 })
            {
                partnerSeeds = body.Partners
                    .Select(p => new ExpenseCouplePartnerSeed(
                        p.Name,
                        p.Color,
                        p.DefaultPct))
                    .ToList();
            }

            IReadOnlyList<ExpenseCoupleExpenseSeed>? expenseSeeds = null;
            if (body.Expenses is { Count: > 0 })
            {
                expenseSeeds = body.Expenses
                    .Select(e => new ExpenseCoupleExpenseSeed(e.Name, e.OccurredOn, e.TotalAmount))
                    .ToList();
            }

            options = new ExpenseCoupleSeedOptions(body.Title, body.Description, partnerSeeds, expenseSeeds);
        }

        var plan = await _db.Plans
            .Include(p => p.Partners)
            .Include(p => p.ExpenseCategories)
            .FirstOrDefaultAsync(p => p.Id == planId && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Plan), planId);

        if (plan.PlanType != PlanType.Expense)
        {
            throw new InvalidOperationException("Bu örnek yalnızca ortak gider planlarına yüklenebilir.");
        }

        var members = await _db.PlanMembers
            .Where(m => m.PlanId == planId)
            .ToListAsync(cancellationToken);
        foreach (var m in members)
        {
            m.PartnerId = null;
            m.UpdatedAtUtc = DateTime.UtcNow;
        }

        var expenseIds = await _db.Expenses
            .Where(e => e.PlanId == planId)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);
        if (expenseIds.Count > 0)
        {
            var shares = await _db.ExpenseShares
                .Where(s => expenseIds.Contains(s.ExpenseId))
                .ToListAsync(cancellationToken);
            _db.ExpenseShares.RemoveRange(shares);
            var existingExpenses = await _db.Expenses
                .Where(e => expenseIds.Contains(e.Id))
                .ToListAsync(cancellationToken);
            _db.Expenses.RemoveRange(existingExpenses);
        }

        var recurrenceIds = await _db.ExpenseRecurrences
            .Where(r => r.PlanId == planId)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);
        if (recurrenceIds.Count > 0)
        {
            var templates = await _db.ExpenseShareTemplates
                .Where(t => recurrenceIds.Contains(t.RecurrenceId))
                .ToListAsync(cancellationToken);
            _db.ExpenseShareTemplates.RemoveRange(templates);
            var existingRecurrences = await _db.ExpenseRecurrences
                .Where(r => recurrenceIds.Contains(r.Id))
                .ToListAsync(cancellationToken);
            _db.ExpenseRecurrences.RemoveRange(existingRecurrences);
        }

        var transfers = await _db.SettlementTransfers
            .Where(t => t.PlanId == planId)
            .ToListAsync(cancellationToken);
        _db.SettlementTransfers.RemoveRange(transfers);

        _db.Partners.RemoveRange(plan.Partners.ToList());
        await _db.SaveChangesAsync(cancellationToken);

        var categoryMap = plan.ExpenseCategories
            .Where(c => !c.IsDeleted)
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        var (title, description, partners, expenses, recurrences, seededTransfers) =
            ExpensePlanTemplateCatalog.Materialize(templateKey, plan.Id, categoryMap, options);

        plan.Title = title;
        plan.Description = description;
        plan.UpdatedAtUtc = DateTime.UtcNow;

        foreach (var partner in partners)
        {
            _db.Partners.Add(partner);
        }

        foreach (var expense in expenses)
        {
            _db.Expenses.Add(expense);
        }

        foreach (var recurrence in recurrences)
        {
            _db.ExpenseRecurrences.Add(recurrence);
        }

        foreach (var transfer in seededTransfers)
        {
            _db.SettlementTransfers.Add(transfer);
        }

        await _db.SaveChangesAsync(cancellationToken);

        var owner = members.FirstOrDefault(m => m.Role == PlanMemberRole.Owner && !m.IsDeleted);
        if (owner is not null && partners.Count > 0)
        {
            owner.PartnerId = partners[0].Id;
            owner.UpdatedAtUtc = DateTime.UtcNow;
        }

        PlanActivity.Write(_db, _currentUser, plan.Id, PlanActivityType.PlanSeeded, $"Örnek yüklendi: {meta.Title}");
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
        if (ExpensePlanTemplateCatalog.IsExpenseTemplate(request.TemplateKey))
        {
            return Task.FromResult(ToExpensePreviewDto(request.TemplateKey));
        }

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

    private static PlanTemplatePreviewDto ToExpensePreviewDto(string key)
    {
        var preview = ExpensePlanTemplateCatalog.BuildPreview(key);
        var partnerCount = Math.Max(preview.Partners.Count, 1);
        var partners = preview.Partners
            .Select(p => new TemplatePartnerPreviewDto(p.Name, p.Color, p.DefaultPct))
            .ToList();
        var rows = preview.Rows
            .Select((row, idx) => new TemplateInstallmentPreviewDto(
                idx + 1,
                row.Name,
                row.Date,
                row.TotalAmount,
                Math.Round(row.TotalAmount / partnerCount, 2)))
            .ToList();

        return new PlanTemplatePreviewDto(
            preview.Key,
            preview.Title,
            preview.Description,
            preview.GrandTotal,
            preview.Rows.Count(r => r.Kind == "expense"),
            null,
            -1,
            partners.Count,
            partners,
            rows);
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
