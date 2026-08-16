using FluentValidation;
using FuzulTaksitTakip.Application.Common.Exceptions;
using FuzulTaksitTakip.Application.Common.Interfaces;
using FuzulTaksitTakip.Application.Common.Mapping;
using FuzulTaksitTakip.Application.Common.Models;
using FuzulTaksitTakip.Domain.Entities;
using FuzulTaksitTakip.Domain.Enums;
using FuzulTaksitTakip.Domain.Services;
using FuzulTaksitTakip.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FuzulTaksitTakip.Application.Plans;

public sealed record GetPlanQuery(Guid PlanId) : IRequest<PlanDto>;

public sealed class GetPlanQueryHandler : IRequestHandler<GetPlanQuery, PlanDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;

    public GetPlanQueryHandler(IAppDbContext db, IPlanAuthorization auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task<PlanDto> Handle(GetPlanQuery request, CancellationToken cancellationToken)
    {
        await _auth.EnsureMemberAsync(request.PlanId, cancellationToken);

        var plan = await _db.Plans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PlanId && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Plan), request.PlanId);

        return plan.ToDto();
    }
}

public sealed record CreatePlanCommand(string Title, string Description, PlanType PlanType = PlanType.Installment)
    : IRequest<PlanDto>;

public sealed class CreatePlanCommandValidator : AbstractValidator<CreatePlanCommand>
{
    public CreatePlanCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.PlanType).IsInEnum();
    }
}

public sealed class CreatePlanCommandHandler : IRequestHandler<CreatePlanCommand, PlanDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CreatePlanCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PlanDto> Handle(CreatePlanCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var plan = new Plan
        {
            OwnerUserId = userId,
            PlanType = request.PlanType,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? string.Empty
        };

        _db.Plans.Add(plan);
        _db.PlanMembers.Add(new PlanMember
        {
            PlanId = plan.Id,
            UserId = userId,
            Role = PlanMemberRole.Owner,
            PartnerId = null
        });

        if (plan.PlanType == PlanType.Expense)
        {
            SeedDefaultExpenseCategories(plan.Id);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return plan.ToDto();
    }

    private void SeedDefaultExpenseCategories(Guid planId)
    {
        var defaults = new (string Name, string Color)[]
        {
            ("Fatura", "#f59e0b"),
            ("Mutfak", "#10b981"),
            ("Market", "#6366f1"),
            ("Ulaşım", "#0ea5e9"),
            ("Diğer", "#94a3b8"),
        };
        var order = 0;
        foreach (var (name, color) in defaults)
        {
            _db.ExpenseCategories.Add(new ExpenseCategory
            {
                PlanId = planId,
                Name = name,
                Color = color,
                SortOrder = order++,
            });
        }
    }
}

public sealed record UpdatePlanCommand(
    Guid PlanId,
    string Title,
    string Description,
    Guid? DeliveryInstallmentId,
    bool RequireReceipt,
    IbanMode IbanMode,
    string? SettlementIban,
    bool RemindersEnabled,
    IReadOnlyList<int> ReminderDaysBefore,
    IReadOnlyList<int> ReminderDaysAfter) : IRequest<PlanDto>;

public sealed class UpdatePlanCommandValidator : AbstractValidator<UpdatePlanCommand>
{
    public UpdatePlanCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.IbanMode).IsInEnum();
        RuleFor(x => x)
            .Must(x => !x.RequireReceipt || x.IbanMode != IbanMode.None)
            .WithMessage("Dekont zorunlu iken IBAN modu seçilmelidir.");
        RuleFor(x => x.SettlementIban)
            .Must(iban => iban is null || IbanNormalizer.IsValidTurkishIban(iban))
            .WithMessage("Geçerli bir TR IBAN girin.");
        RuleFor(x => x)
            .Must(x => x.IbanMode != IbanMode.Plan || IbanNormalizer.IsValidTurkishIban(x.SettlementIban))
            .WithMessage("Plan IBAN modunda settlement IBAN zorunludur.");
        RuleFor(x => x.ReminderDaysBefore)
            .Must(ValidOffsets)
            .WithMessage("Hatırlatma (önce) günleri 0–90 arası, en fazla 8 benzersiz değer olmalıdır.");
        RuleFor(x => x.ReminderDaysAfter)
            .Must(ValidOffsets)
            .WithMessage("Hatırlatma (sonra) günleri 0–90 arası, en fazla 8 benzersiz değer olmalıdır.");
    }

    private static bool ValidOffsets(IReadOnlyList<int>? days)
    {
        if (days is null || days.Count == 0)
        {
            return true;
        }

        if (days.Count > 8 || days.Distinct().Count() != days.Count)
        {
            return false;
        }

        return days.All(d => d is >= 0 and <= 90);
    }
}

public sealed class UpdatePlanCommandHandler : IRequestHandler<UpdatePlanCommand, PlanDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;

    public UpdatePlanCommandHandler(IAppDbContext db, IPlanAuthorization auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task<PlanDto> Handle(UpdatePlanCommand request, CancellationToken cancellationToken)
    {
        await _auth.EnsureOwnerAsync(request.PlanId, cancellationToken);

        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken)
            ?? throw new NotFoundException(nameof(Plan), request.PlanId);

        if (request.DeliveryInstallmentId is Guid deliveryId)
        {
            var exists = await _db.Installments.AnyAsync(
                i => i.Id == deliveryId && i.PlanId == request.PlanId,
                cancellationToken);
            if (!exists)
            {
                throw new ValidationException("DeliveryInstallmentId must belong to this plan.");
            }
        }

        plan.Title = request.Title.Trim();
        plan.Description = request.Description?.Trim() ?? string.Empty;
        plan.DeliveryInstallmentId = request.DeliveryInstallmentId;
        plan.RequireReceipt = request.RequireReceipt;
        plan.IbanMode = request.IbanMode;
        plan.SettlementIban = request.IbanMode == IbanMode.Plan
            ? IbanNormalizer.Normalize(request.SettlementIban)
            : null;
        plan.RemindersEnabled = request.RemindersEnabled;
        plan.ReminderDaysBefore = NormalizeOffsets(request.ReminderDaysBefore);
        plan.ReminderDaysAfter = NormalizeOffsets(request.ReminderDaysAfter);
        plan.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return plan.ToDto();
    }

    private static int[] NormalizeOffsets(IReadOnlyList<int>? days) =>
        (days ?? Array.Empty<int>())
        .Where(d => d is >= 0 and <= 90)
        .Distinct()
        .OrderBy(d => d)
        .Take(8)
        .ToArray();
}

public sealed record DeletePlanCommand(Guid PlanId) : IRequest;

public sealed class DeletePlanCommandHandler : IRequestHandler<DeletePlanCommand>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;

    public DeletePlanCommandHandler(IAppDbContext db, IPlanAuthorization auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task Handle(DeletePlanCommand request, CancellationToken cancellationToken)
    {
        await _auth.EnsureOwnerAsync(request.PlanId, cancellationToken);

        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken)
            ?? throw new NotFoundException(nameof(Plan), request.PlanId);

        // Avoid FK conflicts when cascade-deleting installments referenced as delivery month.
        plan.DeliveryInstallmentId = null;

        // Clear partner FKs that use Restrict (expense shares / transfers / paid-by) before plan cascade.
        await ClearExpensePartnerDependenciesAsync(request.PlanId, cancellationToken);

        var members = await _db.PlanMembers
            .Where(m => m.PlanId == request.PlanId)
            .ToListAsync(cancellationToken);
        foreach (var m in members)
        {
            m.PartnerId = null;
        }

        await _db.SaveChangesAsync(cancellationToken);

        _db.Plans.Remove(plan);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task ClearExpensePartnerDependenciesAsync(Guid planId, CancellationToken cancellationToken)
    {
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

            var expensePayments = await _db.ExpensePayments
                .Where(p => expenseIds.Contains(p.ExpenseId))
                .ToListAsync(cancellationToken);
            _db.ExpensePayments.RemoveRange(expensePayments);

            var expenses = await _db.Expenses
                .Where(e => e.PlanId == planId)
                .ToListAsync(cancellationToken);
            foreach (var expense in expenses)
            {
                expense.PaidByPartnerId = null;
            }
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

            var recurrences = await _db.ExpenseRecurrences
                .Where(r => r.PlanId == planId)
                .ToListAsync(cancellationToken);
            foreach (var recurrence in recurrences)
            {
                recurrence.DefaultPaidByPartnerId = null;
            }
        }

        var transfers = await _db.SettlementTransfers
            .Where(t => t.PlanId == planId)
            .ToListAsync(cancellationToken);
        _db.SettlementTransfers.RemoveRange(transfers);

        var invites = await _db.PlanInvites
            .Where(i => i.PlanId == planId)
            .ToListAsync(cancellationToken);
        _db.PlanInvites.RemoveRange(invites);

        // Installment payments/shares also Restrict partner deletes during plan cascade.
        var installmentIds = await _db.Installments
            .Where(i => i.PlanId == planId)
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);
        if (installmentIds.Count > 0)
        {
            var payments = await _db.Payments
                .Where(p => installmentIds.Contains(p.InstallmentId))
                .ToListAsync(cancellationToken);
            _db.Payments.RemoveRange(payments);

            var installmentShares = await _db.InstallmentShares
                .Where(s => installmentIds.Contains(s.InstallmentId))
                .ToListAsync(cancellationToken);
            _db.InstallmentShares.RemoveRange(installmentShares);
        }
    }
}

public sealed record ArchivePlanCommand(Guid PlanId) : IRequest;

public sealed class ArchivePlanCommandHandler : IRequestHandler<ArchivePlanCommand>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;
    private readonly ICurrentUser _currentUser;

    public ArchivePlanCommandHandler(IAppDbContext db, IPlanAuthorization auth, ICurrentUser currentUser)
    {
        _db = db;
        _auth = auth;
        _currentUser = currentUser;
    }

    public async Task Handle(ArchivePlanCommand request, CancellationToken cancellationToken)
    {
        await _auth.EnsureOwnerAsync(request.PlanId, cancellationToken);

        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == request.PlanId && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Plan), request.PlanId);

        plan.IsDeleted = true;
        plan.UpdatedAtUtc = DateTime.UtcNow;
        PlanActivity.Write(_db, _currentUser, plan.Id, PlanActivityType.PlanArchived, $"Plan arşivlendi: {plan.Title}");
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public sealed record RestorePlanCommand(Guid PlanId) : IRequest<PlanDto>;

public sealed class RestorePlanCommandHandler : IRequestHandler<RestorePlanCommand, PlanDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;
    private readonly ICurrentUser _currentUser;

    public RestorePlanCommandHandler(IAppDbContext db, IPlanAuthorization auth, ICurrentUser currentUser)
    {
        _db = db;
        _auth = auth;
        _currentUser = currentUser;
    }

    public async Task<PlanDto> Handle(RestorePlanCommand request, CancellationToken cancellationToken)
    {
        await _auth.EnsureOwnerAsync(request.PlanId, cancellationToken);

        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == request.PlanId && p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Plan), request.PlanId);

        plan.IsDeleted = false;
        plan.UpdatedAtUtc = DateTime.UtcNow;
        PlanActivity.Write(_db, _currentUser, plan.Id, PlanActivityType.PlanRestored, $"Plan geri yüklendi: {plan.Title}");
        await _db.SaveChangesAsync(cancellationToken);
        return plan.ToDto();
    }
}

public sealed record CopyPlanCommand(Guid PlanId) : IRequest<PlanDto>;

public sealed class CopyPlanCommandHandler : IRequestHandler<CopyPlanCommand, PlanDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;
    private readonly ICurrentUser _currentUser;

    public CopyPlanCommandHandler(IAppDbContext db, IPlanAuthorization auth, ICurrentUser currentUser)
    {
        _db = db;
        _auth = auth;
        _currentUser = currentUser;
    }

    public async Task<PlanDto> Handle(CopyPlanCommand request, CancellationToken cancellationToken)
    {
        await _auth.EnsureOwnerAsync(request.PlanId, cancellationToken);
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var source = await _db.Plans
            .AsNoTracking()
            .Include(p => p.Partners)
            .Include(p => p.Installments).ThenInclude(i => i.CustomShares)
            .FirstOrDefaultAsync(p => p.Id == request.PlanId && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Plan), request.PlanId);

        var copy = new Plan
        {
            OwnerUserId = userId,
            PlanType = source.PlanType,
            Title = $"{source.Title} (kopya)",
            Description = source.Description,
            RequireReceipt = source.RequireReceipt,
            IbanMode = source.IbanMode,
            SettlementIban = source.SettlementIban,
            RemindersEnabled = source.RemindersEnabled,
            ReminderDaysBefore = source.ReminderDaysBefore?.ToArray() ?? Array.Empty<int>(),
            ReminderDaysAfter = source.ReminderDaysAfter?.ToArray() ?? Array.Empty<int>()
        };
        _db.Plans.Add(copy);
        _db.PlanMembers.Add(new PlanMember
        {
            PlanId = copy.Id,
            UserId = userId,
            Role = PlanMemberRole.Owner
        });

        var partnerMap = new Dictionary<Guid, Guid>();
        foreach (var p in source.Partners.OrderBy(x => x.SortOrder))
        {
            var np = new Partner
            {
                PlanId = copy.Id,
                Name = p.Name,
                Color = p.Color,
                DefaultPct = p.DefaultPct,
                SortOrder = p.SortOrder,
                Iban = p.Iban
            };
            partnerMap[p.Id] = np.Id;
            _db.Partners.Add(np);
        }

        Guid? deliveryId = null;
        foreach (var inst in source.Installments.OrderBy(i => i.SortOrder))
        {
            var ni = new Installment
            {
                PlanId = copy.Id,
                Name = inst.Name,
                DueDate = inst.DueDate,
                TotalAmount = inst.TotalAmount,
                ShareType = inst.ShareType,
                SortOrder = inst.SortOrder
            };
            foreach (var share in inst.CustomShares)
            {
                if (!partnerMap.TryGetValue(share.PartnerId, out var newPartnerId))
                {
                    continue;
                }

                ni.CustomShares.Add(new InstallmentShare
                {
                    InstallmentId = ni.Id,
                    PartnerId = newPartnerId,
                    Amount = share.Amount
                });
            }

            _db.Installments.Add(ni);
            if (source.DeliveryInstallmentId == inst.Id)
            {
                deliveryId = ni.Id;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        copy.DeliveryInstallmentId = deliveryId;
        PlanActivity.Write(_db, _currentUser, copy.Id, PlanActivityType.PlanCopied, $"Plan kopyalandı: {source.Title}");
        await _db.SaveChangesAsync(cancellationToken);
        return copy.ToDto();
    }
}
