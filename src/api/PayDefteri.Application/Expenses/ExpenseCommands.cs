using FluentValidation;
using PayDefteri.Application.Common.Exceptions;
using PayDefteri.Application.Common.Interfaces;
using PayDefteri.Application.Common.Mapping;
using PayDefteri.Application.Common.Models;
using PayDefteri.Domain.Entities;
using PayDefteri.Domain.Enums;
using PayDefteri.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace PayDefteri.Application.Expenses;

internal static class ExpensePlanGuards
{
    public static async Task<Plan> EnsureExpensePlanAsync(
        IAppDbContext db,
        IPlanAuthorization auth,
        Guid planId,
        CancellationToken ct,
        bool requireOwner = false)
    {
        if (requireOwner)
        {
            await auth.EnsureOwnerAsync(planId, ct);
        }
        else
        {
            await auth.EnsureMemberAsync(planId, ct);
        }

        var plan = await db.Plans.FirstOrDefaultAsync(p => p.Id == planId && !p.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Plan), planId);
        if (plan.PlanType != PlanType.Expense)
        {
            throw new ValidationException("Bu işlem yalnızca gider planlarında kullanılabilir.");
        }

        return plan;
    }
}

internal static class ExpenseAuthorization
{
    public static async Task EnsureCanManageAsync(
        Expense expense,
        Guid planId,
        IPlanAuthorization auth,
        ICurrentUser currentUser,
        CancellationToken ct)
    {
        if (await auth.IsOwnerAsync(planId, ct))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(currentUser.UserId)
            && string.Equals(expense.CreatedByUserId, currentUser.UserId, StringComparison.Ordinal))
        {
            return;
        }

        throw new ForbiddenException("Yalnızca eklediğiniz giderleri düzenleyebilirsiniz.");
    }
}

public sealed record GetExpenseBoardQuery(Guid PlanId) : IRequest<ExpenseBoardDto>;

public sealed record ListExpensesQuery(Guid PlanId, int Page = 1, int PageSize = 50) : IRequest<PagedExpenseDto>;

public sealed class ListExpensesQueryHandler : IRequestHandler<ListExpensesQuery, PagedExpenseDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;
    private readonly ICurrentUser _currentUser;

    public ListExpensesQueryHandler(IAppDbContext db, IPlanAuthorization auth, ICurrentUser currentUser)
    {
        _db = db;
        _auth = auth;
        _currentUser = currentUser;
    }

    public async Task<PagedExpenseDto> Handle(ListExpensesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        await ExpensePlanGuards.EnsureExpensePlanAsync(_db, _auth, request.PlanId, cancellationToken);

        var partners = await _db.Partners.AsNoTracking()
            .Where(p => p.PlanId == request.PlanId && !p.IsDeleted)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(cancellationToken);
        var totalCount = await _db.Expenses.CountAsync(e => e.PlanId == request.PlanId && !e.IsDeleted, cancellationToken);
        var isOwner = await _auth.IsOwnerAsync(request.PlanId, cancellationToken);
        var expenses = await _db.Expenses.AsNoTracking()
            .Include(e => e.CustomShares)
            .Include(e => e.Payments)
            .Include(e => e.Category)
            .Where(e => e.PlanId == request.PlanId && !e.IsDeleted)
            .OrderByDescending(e => e.OccurredOn)
            .ThenByDescending(e => e.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedExpenseDto(
            expenses.Select(e => ExpenseMappings.ToDto(e, partners,
                isOwner || string.Equals(e.CreatedByUserId, _currentUser.UserId, StringComparison.Ordinal))).ToList(),
            page,
            pageSize,
            totalCount);
    }
}

public sealed class GetExpenseBoardQueryHandler : IRequestHandler<GetExpenseBoardQuery, ExpenseBoardDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;
    private readonly ICurrentUser _currentUser;

    public GetExpenseBoardQueryHandler(IAppDbContext db, IPlanAuthorization auth, ICurrentUser currentUser)
    {
        _db = db;
        _auth = auth;
        _currentUser = currentUser;
    }

    public async Task<ExpenseBoardDto> Handle(GetExpenseBoardQuery request, CancellationToken cancellationToken)
    {
        var plan = await ExpensePlanGuards.EnsureExpensePlanAsync(_db, _auth, request.PlanId, cancellationToken);

        var partners = await _db.Partners.AsNoTracking()
            .Where(p => p.PlanId == plan.Id && !p.IsDeleted)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(cancellationToken);

        var categories = await _db.ExpenseCategories.AsNoTracking()
            .Where(c => c.PlanId == plan.Id && !c.IsDeleted)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(cancellationToken);

        var expenses = await _db.Expenses.AsNoTracking()
            .Include(e => e.CustomShares)
            .Include(e => e.Payments)
            .Include(e => e.Category)
            .Where(e => e.PlanId == plan.Id && !e.IsDeleted)
            .OrderByDescending(e => e.OccurredOn)
            .ThenByDescending(e => e.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var recurrences = await _db.ExpenseRecurrences.AsNoTracking()
            .Include(r => r.CustomShares)
            .Where(r => r.PlanId == plan.Id && !r.IsDeleted)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        var transfers = await _db.SettlementTransfers.AsNoTracking()
            .Where(t => t.PlanId == plan.Id && !t.IsDeleted)
            .OrderByDescending(t => t.TransferredOn)
            .ThenByDescending(t => t.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var balances = ExpenseSettlementCalculator.ComputeBalances(expenses, transfers, partners);
        var isOwner = await _auth.IsOwnerAsync(request.PlanId, cancellationToken);

        return new ExpenseBoardDto(
            plan.ToDto(),
            partners.Select(p => new ExpenseBalanceDto(
                p.Id,
                p.Name,
                p.Color,
                balances.GetValueOrDefault(p.Id))).ToList(),
            expenses.Select(e => ExpenseMappings.ToDto(
                e,
                partners,
                isOwner || (!string.IsNullOrWhiteSpace(_currentUser.UserId)
                            && string.Equals(e.CreatedByUserId, _currentUser.UserId, StringComparison.Ordinal)))).ToList(),
            categories.Select(c => new ExpenseCategoryDto(c.Id, c.PlanId, c.Name, c.Color, c.SortOrder)).ToList(),
            recurrences.Select(ExpenseMappings.ToDto).ToList(),
            transfers.Select(t =>
            {
                var from = partners.FirstOrDefault(p => p.Id == t.FromPartnerId);
                var to = partners.FirstOrDefault(p => p.Id == t.ToPartnerId);
                return new SettlementTransferDto(
                    t.Id,
                    t.PlanId,
                    t.FromPartnerId,
                    from?.Name ?? "?",
                    t.ToPartnerId,
                    to?.Name ?? "?",
                    t.Amount,
                    t.TransferredOn,
                    t.Note);
            }).ToList(),
            isOwner);
    }
}

public sealed record CreateExpenseCommand(
    Guid PlanId,
    string Name,
    DateOnly OccurredOn,
    decimal TotalAmount,
    ShareType ShareType,
    ExpenseStatus Status,
    Guid? PaidByPartnerId,
    Guid? CategoryId,
    string? Note,
    IReadOnlyList<CustomShareDto>? CustomShares,
    IReadOnlyList<ExpensePaymentDto>? Payments,
    int InstallmentCount = 1) : IRequest<ExpenseDto>;

public sealed class CreateExpenseCommandValidator : AbstractValidator<CreateExpenseCommand>
{
    public CreateExpenseCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.TotalAmount).GreaterThan(0);
        RuleFor(x => x.ShareType).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Note).MaximumLength(2000);
        RuleFor(x => x.InstallmentCount).InclusiveBetween(1, 120);
        RuleFor(x => x)
            .Must(x => x.Status != ExpenseStatus.Paid
                       || ExpensePaymentHelpers.HasAnyPayer(x.Payments, x.PaidByPartnerId))
            .WithMessage("Ödendi durumunda en az bir ödeyen zorunludur.");
    }
}

public sealed class CreateExpenseCommandHandler : IRequestHandler<CreateExpenseCommand, ExpenseDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;
    private readonly ICurrentUser _currentUser;

    public CreateExpenseCommandHandler(IAppDbContext db, IPlanAuthorization auth, ICurrentUser currentUser)
    {
        _db = db;
        _auth = auth;
        _currentUser = currentUser;
    }

    public async Task<ExpenseDto> Handle(CreateExpenseCommand request, CancellationToken cancellationToken)
    {
        await ExpensePlanGuards.EnsureExpensePlanAsync(_db, _auth, request.PlanId, cancellationToken);
        var currentUserId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var partners = await _db.Partners
            .Where(p => p.PlanId == request.PlanId && !p.IsDeleted)
            .ToListAsync(cancellationToken);
        if (partners.Count == 0)
        {
            throw new ValidationException("Önce en az bir ortak ekleyin.");
        }

        if (request.CategoryId is Guid catId)
        {
            var catOk = await _db.ExpenseCategories.AnyAsync(
                c => c.Id == catId && c.PlanId == request.PlanId && !c.IsDeleted, cancellationToken);
            if (!catOk)
            {
                throw new ValidationException("Kategori bulunamadı.");
            }
        }

        var totalAmount = decimal.Round(request.TotalAmount, 2);
        if (request.ShareType == ShareType.Custom
            && !CustomSharesMatchTotal(request.CustomShares, partners, totalAmount))
        {
            throw new ValidationException("Özel payların toplamı tutara eşit olmalıdır.");
        }

        if (request.ShareType == ShareType.Default && !ShareCalculator.DefaultPercentagesSumTo100(partners))
        {
            throw new ValidationException("Varsayılan pay yüzdeleri toplamı 100 olmalıdır.");
        }

        var installmentAmounts = SplitAmount(totalAmount, request.InstallmentCount);
        var expenses = new List<Expense>(request.InstallmentCount);
        for (var index = 0; index < request.InstallmentCount; index++)
        {
            var installmentAmount = installmentAmounts[index];
            var isFirstInstallment = index == 0;
            var expense = new Expense
            {
                PlanId = request.PlanId,
                CreatedByUserId = currentUserId,
                Name = request.InstallmentCount == 1
                    ? request.Name.Trim()
                    : $"{request.Name.Trim()} — {index + 1}. taksit",
                OccurredOn = request.OccurredOn.AddMonths(index),
                TotalAmount = installmentAmount,
                ShareType = request.ShareType,
                Status = isFirstInstallment ? request.Status : ExpenseStatus.Planned,
                CategoryId = request.CategoryId,
                Note = request.Note?.Trim() ?? string.Empty,
            };

            ApplyCustomShares(
                expense,
                SplitCustomShares(request.CustomShares, installmentAmounts, index, totalAmount),
                partners);

            if (isFirstInstallment && request.Status == ExpenseStatus.Paid)
            {
                ExpensePaymentHelpers.ApplyPayments(
                    expense,
                    ScalePayments(request.Payments, installmentAmount, totalAmount),
                    request.PaidByPartnerId,
                    partners,
                    persistViaDb: null);
            }

            expenses.Add(expense);
        }

        _db.Expenses.AddRange(expenses);
        await _db.SaveChangesAsync(cancellationToken);

        var firstExpense = expenses[0];
        firstExpense.Category = request.CategoryId is null
            ? null
            : await _db.ExpenseCategories.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

        return ExpenseMappings.ToDto(firstExpense, partners, canManage: true);
    }

    private static IReadOnlyList<decimal> SplitAmount(decimal total, int count)
    {
        var baseAmount = decimal.Floor(total / count * 100m) / 100m;
        var amounts = Enumerable.Repeat(baseAmount, count).ToArray();
        amounts[^1] = decimal.Round(total - baseAmount * (count - 1), 2);
        return amounts;
    }

    private static bool CustomSharesMatchTotal(
        IReadOnlyList<CustomShareDto>? shares,
        IReadOnlyList<Partner> partners,
        decimal total)
    {
        if (shares is null)
        {
            return false;
        }

        var partnerIds = partners.Select(p => p.Id).ToHashSet();
        return shares.All(s => partnerIds.Contains(s.PartnerId))
               && Math.Abs(shares.Sum(s => decimal.Round(s.Amount, 2)) - total) <= 0.01m;
    }

    private static IReadOnlyList<CustomShareDto>? SplitCustomShares(
        IReadOnlyList<CustomShareDto>? shares,
        IReadOnlyList<decimal> installmentAmounts,
        int installmentIndex,
        decimal totalAmount)
    {
        if (shares is null)
        {
            return null;
        }

        var result = new List<CustomShareDto>(shares.Count);
        var installmentAmount = installmentAmounts[installmentIndex];
        var remaining = installmentAmount;
        for (var index = 0; index < shares.Count; index++)
        {
            var share = shares[index];
            decimal amount;
            if (index == shares.Count - 1)
            {
                amount = remaining;
            }
            else if (installmentIndex == installmentAmounts.Count - 1)
            {
                var allocatedPreviously = installmentAmounts
                    .Take(installmentIndex)
                    .Sum(part => decimal.Floor(share.Amount / totalAmount * part * 100m) / 100m);
                amount = decimal.Round(share.Amount - allocatedPreviously, 2);
            }
            else
            {
                amount = decimal.Floor(share.Amount / totalAmount * installmentAmount * 100m) / 100m;
            }
            remaining -= amount;
            result.Add(new CustomShareDto(share.PartnerId, amount));
        }

        return result;
    }

    private static IReadOnlyList<ExpensePaymentDto>? ScalePayments(
        IReadOnlyList<ExpensePaymentDto>? payments,
        decimal installmentAmount,
        decimal totalAmount)
    {
        if (payments is null)
        {
            return null;
        }

        var positivePayments = payments.Where(p => p.Amount > 0m).ToList();
        var result = new List<ExpensePaymentDto>(positivePayments.Count);
        var remaining = installmentAmount;
        for (var index = 0; index < positivePayments.Count; index++)
        {
            var payment = positivePayments[index];
            var amount = index == positivePayments.Count - 1
                ? remaining
                : decimal.Round(payment.Amount / totalAmount * installmentAmount, 2);
            remaining -= amount;
            result.Add(new ExpensePaymentDto(payment.PartnerId, amount));
        }

        return result;
    }

    internal static void ApplyCustomShares(
        Expense expense,
        IReadOnlyList<CustomShareDto>? shares,
        IReadOnlyList<Partner> partners)
    {
        expense.CustomShares.Clear();
        if (expense.ShareType != ShareType.Custom || shares is null)
        {
            return;
        }

        foreach (var s in shares)
        {
            if (partners.All(p => p.Id != s.PartnerId))
            {
                continue;
            }

            expense.CustomShares.Add(new ExpenseShare
            {
                ExpenseId = expense.Id,
                PartnerId = s.PartnerId,
                Amount = decimal.Round(s.Amount, 2),
            });
        }
    }
}

public sealed record UpdateExpenseCommand(
    Guid PlanId,
    Guid ExpenseId,
    string Name,
    DateOnly OccurredOn,
    decimal TotalAmount,
    ShareType ShareType,
    ExpenseStatus Status,
    Guid? PaidByPartnerId,
    Guid? CategoryId,
    string? Note,
    IReadOnlyList<CustomShareDto>? CustomShares,
    IReadOnlyList<ExpensePaymentDto>? Payments) : IRequest<ExpenseDto>;

public sealed class UpdateExpenseCommandValidator : AbstractValidator<UpdateExpenseCommand>
{
    public UpdateExpenseCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.TotalAmount).GreaterThan(0);
        RuleFor(x => x)
            .Must(x => x.Status != ExpenseStatus.Paid
                       || ExpensePaymentHelpers.HasAnyPayer(x.Payments, x.PaidByPartnerId))
            .WithMessage("Ödendi durumunda en az bir ödeyen zorunludur.");
    }
}

public sealed class UpdateExpenseCommandHandler : IRequestHandler<UpdateExpenseCommand, ExpenseDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;
    private readonly ICurrentUser _currentUser;

    public UpdateExpenseCommandHandler(IAppDbContext db, IPlanAuthorization auth, ICurrentUser currentUser)
    {
        _db = db;
        _auth = auth;
        _currentUser = currentUser;
    }

    public async Task<ExpenseDto> Handle(UpdateExpenseCommand request, CancellationToken cancellationToken)
    {
        await ExpensePlanGuards.EnsureExpensePlanAsync(_db, _auth, request.PlanId, cancellationToken);

        var expense = await _db.Expenses
            .Include(e => e.CustomShares)
            .Include(e => e.Payments)
            .FirstOrDefaultAsync(e => e.Id == request.ExpenseId && e.PlanId == request.PlanId && !e.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Expense), request.ExpenseId);
        await ExpenseAuthorization.EnsureCanManageAsync(expense, request.PlanId, _auth, _currentUser, cancellationToken);

        var partners = await _db.Partners
            .Where(p => p.PlanId == request.PlanId && !p.IsDeleted)
            .ToListAsync(cancellationToken);

        return await _db.ExecuteInTransactionAsync(async transactionCt =>
        {
        expense.Name = request.Name.Trim();
        expense.OccurredOn = request.OccurredOn;
        expense.TotalAmount = decimal.Round(request.TotalAmount, 2);
        expense.ShareType = request.ShareType;
        expense.Status = request.Status;
        expense.CategoryId = request.CategoryId;
        expense.Note = request.Note?.Trim() ?? string.Empty;
        expense.UpdatedAtUtc = DateTime.UtcNow;

        // Replace shares via DbSet DELETE + INSERT. Navigation Clear()+Add on a tracked
        // expense can mark new Guid-keyed shares as Modified → UPDATE 0 rows → 409.
        var existing = expense.CustomShares.ToList();
        if (existing.Count > 0)
        {
            _db.ExpenseShares.RemoveRange(existing);
        }

        expense.CustomShares.Clear();

        var existingPayments = expense.Payments.ToList();
        if (existingPayments.Count > 0)
        {
            _db.ExpensePayments.RemoveRange(existingPayments);
        }

        expense.Payments.Clear();
        await _db.SaveChangesAsync(transactionCt);

        var builtShares = new List<ExpenseShare>();
        if (request.ShareType == ShareType.Custom && request.CustomShares is not null)
        {
            foreach (var s in request.CustomShares)
            {
                if (partners.All(p => p.Id != s.PartnerId))
                {
                    continue;
                }

                var share = new ExpenseShare
                {
                    Id = Guid.NewGuid(),
                    ExpenseId = expense.Id,
                    PartnerId = s.PartnerId,
                    Amount = decimal.Round(s.Amount, 2),
                };
                _db.ExpenseShares.Add(share);
                builtShares.Add(share);
            }
        }

        if (request.ShareType == ShareType.Custom)
        {
            var sum = builtShares.Sum(s => s.Amount);
            if (Math.Abs(sum - expense.TotalAmount) > 0.01m)
            {
                throw new ValidationException("Özel payların toplamı tutara eşit olmalıdır.");
            }
        }

        ExpensePaymentHelpers.ApplyPayments(
            expense,
            request.Payments,
            request.PaidByPartnerId,
            partners,
            persistViaDb: _db);

        await _db.SaveChangesAsync(transactionCt);

        var mapped = await _db.Expenses
            .AsNoTracking()
            .Include(e => e.CustomShares)
            .Include(e => e.Payments)
            .Include(e => e.Category)
            .FirstAsync(e => e.Id == expense.Id, transactionCt);

        return ExpenseMappings.ToDto(mapped, partners, canManage: true);
        }, cancellationToken);
    }
}

public sealed record DeleteExpenseCommand(Guid PlanId, Guid ExpenseId) : IRequest;

public sealed class DeleteExpenseCommandHandler : IRequestHandler<DeleteExpenseCommand>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;
    private readonly ICurrentUser _currentUser;

    public DeleteExpenseCommandHandler(IAppDbContext db, IPlanAuthorization auth, ICurrentUser currentUser)
    {
        _db = db;
        _auth = auth;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteExpenseCommand request, CancellationToken cancellationToken)
    {
        await ExpensePlanGuards.EnsureExpensePlanAsync(_db, _auth, request.PlanId, cancellationToken);

        var expense = await _db.Expenses
            .FirstOrDefaultAsync(e => e.Id == request.ExpenseId && e.PlanId == request.PlanId && !e.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Expense), request.ExpenseId);
        await ExpenseAuthorization.EnsureCanManageAsync(expense, request.PlanId, _auth, _currentUser, cancellationToken);

        expense.IsDeleted = true;
        expense.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public sealed record CreateExpenseCategoryCommand(Guid PlanId, string Name, string Color) : IRequest<ExpenseCategoryDto>;

public sealed class CreateExpenseCategoryCommandHandler : IRequestHandler<CreateExpenseCategoryCommand, ExpenseCategoryDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;

    public CreateExpenseCategoryCommandHandler(IAppDbContext db, IPlanAuthorization auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task<ExpenseCategoryDto> Handle(CreateExpenseCategoryCommand request, CancellationToken cancellationToken)
    {
        await ExpensePlanGuards.EnsureExpensePlanAsync(_db, _auth, request.PlanId, cancellationToken, requireOwner: true);

        var maxOrder = await _db.ExpenseCategories
            .Where(c => c.PlanId == request.PlanId && !c.IsDeleted)
            .Select(c => (int?)c.SortOrder)
            .MaxAsync(cancellationToken) ?? -1;

        var cat = new ExpenseCategory
        {
            PlanId = request.PlanId,
            Name = request.Name.Trim(),
            Color = string.IsNullOrWhiteSpace(request.Color) ? "#94a3b8" : request.Color.Trim(),
            SortOrder = maxOrder + 1,
        };
        _db.ExpenseCategories.Add(cat);
        await _db.SaveChangesAsync(cancellationToken);
        return new ExpenseCategoryDto(cat.Id, cat.PlanId, cat.Name, cat.Color, cat.SortOrder);
    }
}

public sealed record CreateSettlementTransferCommand(
    Guid PlanId,
    Guid FromPartnerId,
    Guid ToPartnerId,
    decimal Amount,
    DateOnly TransferredOn,
    string? Note) : IRequest<SettlementTransferDto>;

public sealed class CreateSettlementTransferCommandValidator : AbstractValidator<CreateSettlementTransferCommand>
{
    public CreateSettlementTransferCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x)
            .Must(x => x.FromPartnerId != x.ToPartnerId)
            .WithMessage("Gönderen ve alan ortak farklı olmalıdır.");
    }
}

public sealed class CreateSettlementTransferCommandHandler
    : IRequestHandler<CreateSettlementTransferCommand, SettlementTransferDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;

    public CreateSettlementTransferCommandHandler(IAppDbContext db, IPlanAuthorization auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task<SettlementTransferDto> Handle(
        CreateSettlementTransferCommand request,
        CancellationToken cancellationToken)
    {
        await ExpensePlanGuards.EnsureExpensePlanAsync(_db, _auth, request.PlanId, cancellationToken, requireOwner: true);

        var partners = await _db.Partners
            .Where(p => p.PlanId == request.PlanId && !p.IsDeleted)
            .ToListAsync(cancellationToken);
        var from = partners.FirstOrDefault(p => p.Id == request.FromPartnerId)
            ?? throw new ValidationException("Gönderen ortak bulunamadı.");
        var to = partners.FirstOrDefault(p => p.Id == request.ToPartnerId)
            ?? throw new ValidationException("Alan ortak bulunamadı.");

        var transfer = new SettlementTransfer
        {
            PlanId = request.PlanId,
            FromPartnerId = from.Id,
            ToPartnerId = to.Id,
            Amount = decimal.Round(request.Amount, 2),
            TransferredOn = request.TransferredOn,
            Note = request.Note?.Trim() ?? string.Empty,
        };
        _db.SettlementTransfers.Add(transfer);
        await _db.SaveChangesAsync(cancellationToken);

        return new SettlementTransferDto(
            transfer.Id,
            transfer.PlanId,
            from.Id,
            from.Name,
            to.Id,
            to.Name,
            transfer.Amount,
            transfer.TransferredOn,
            transfer.Note);
    }
}

public sealed record DeleteSettlementTransferCommand(Guid PlanId, Guid TransferId) : IRequest;

public sealed class DeleteSettlementTransferCommandHandler : IRequestHandler<DeleteSettlementTransferCommand>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;

    public DeleteSettlementTransferCommandHandler(IAppDbContext db, IPlanAuthorization auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task Handle(DeleteSettlementTransferCommand request, CancellationToken cancellationToken)
    {
        await ExpensePlanGuards.EnsureExpensePlanAsync(_db, _auth, request.PlanId, cancellationToken, requireOwner: true);

        var transfer = await _db.SettlementTransfers
            .FirstOrDefaultAsync(t => t.Id == request.TransferId && t.PlanId == request.PlanId && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(SettlementTransfer), request.TransferId);

        transfer.IsDeleted = true;
        transfer.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public sealed record CreateExpenseRecurrenceCommand(
    Guid PlanId,
    string Name,
    decimal TotalAmount,
    ShareType ShareType,
    Guid? CategoryId,
    Guid? DefaultPaidByPartnerId,
    RecurrenceFrequency Frequency,
    int AnchorDay,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? Note,
    IReadOnlyList<CustomShareDto>? CustomShares) : IRequest<ExpenseRecurrenceDto>;

public sealed class CreateExpenseRecurrenceCommandValidator : AbstractValidator<CreateExpenseRecurrenceCommand>
{
    public CreateExpenseRecurrenceCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.TotalAmount).GreaterThan(0);
        RuleFor(x => x.Frequency).IsInEnum();
        RuleFor(x => x.AnchorDay).InclusiveBetween(0, 31);
    }
}

public sealed class CreateExpenseRecurrenceCommandHandler
    : IRequestHandler<CreateExpenseRecurrenceCommand, ExpenseRecurrenceDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;

    public CreateExpenseRecurrenceCommandHandler(IAppDbContext db, IPlanAuthorization auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task<ExpenseRecurrenceDto> Handle(
        CreateExpenseRecurrenceCommand request,
        CancellationToken cancellationToken)
    {
        await ExpensePlanGuards.EnsureExpensePlanAsync(_db, _auth, request.PlanId, cancellationToken, requireOwner: true);

        var partners = await _db.Partners
            .Where(p => p.PlanId == request.PlanId && !p.IsDeleted)
            .ToListAsync(cancellationToken);

        var next = ExpenseRecurrenceCalendar.FirstOnOrAfter(request.Frequency, request.AnchorDay, request.StartDate);
        var recurrence = new ExpenseRecurrence
        {
            PlanId = request.PlanId,
            Name = request.Name.Trim(),
            TotalAmount = decimal.Round(request.TotalAmount, 2),
            ShareType = request.ShareType,
            CategoryId = request.CategoryId,
            DefaultPaidByPartnerId = request.DefaultPaidByPartnerId,
            Frequency = request.Frequency,
            AnchorDay = request.AnchorDay,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            NextOccurrence = next,
            IsActive = true,
            Note = request.Note?.Trim() ?? string.Empty,
        };

        if (request.ShareType == ShareType.Custom && request.CustomShares is not null)
        {
            foreach (var s in request.CustomShares.Where(s => partners.Any(p => p.Id == s.PartnerId)))
            {
                recurrence.CustomShares.Add(new ExpenseShareTemplate
                {
                    RecurrenceId = recurrence.Id,
                    PartnerId = s.PartnerId,
                    Amount = decimal.Round(s.Amount, 2),
                });
            }
        }

        _db.ExpenseRecurrences.Add(recurrence);
        await _db.SaveChangesAsync(cancellationToken);
        await ExpenseRecurrenceGenerator.GenerateDueAsync(
            _db, request.PlanId, DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken);

        var saved = await _db.ExpenseRecurrences.AsNoTracking()
            .Include(r => r.CustomShares)
            .FirstAsync(r => r.Id == recurrence.Id, cancellationToken);
        return ExpenseMappings.ToDto(saved);
    }
}

public sealed record DeleteExpenseRecurrenceCommand(Guid PlanId, Guid RecurrenceId) : IRequest;

public sealed class DeleteExpenseRecurrenceCommandHandler : IRequestHandler<DeleteExpenseRecurrenceCommand>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;

    public DeleteExpenseRecurrenceCommandHandler(IAppDbContext db, IPlanAuthorization auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task Handle(DeleteExpenseRecurrenceCommand request, CancellationToken cancellationToken)
    {
        await ExpensePlanGuards.EnsureExpensePlanAsync(_db, _auth, request.PlanId, cancellationToken, requireOwner: true);

        var row = await _db.ExpenseRecurrences
            .FirstOrDefaultAsync(r => r.Id == request.RecurrenceId && r.PlanId == request.PlanId && !r.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(ExpenseRecurrence), request.RecurrenceId);

        row.IsActive = false;
        row.IsDeleted = true;
        row.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}

internal static class ExpensePaymentHelpers
{
    public static bool HasAnyPayer(IReadOnlyList<ExpensePaymentDto>? payments, Guid? paidByPartnerId)
        => (payments is not null && payments.Any(p => p.Amount > 0m)) || paidByPartnerId is not null;

    /// <summary>
    /// Resolves payment lines from explicit payments or legacy single PaidByPartnerId.
    /// When Status is Planned, clears payments. When Paid, payments must sum to TotalAmount.
    /// </summary>
    public static void ApplyPayments(
        Expense expense,
        IReadOnlyList<ExpensePaymentDto>? payments,
        Guid? paidByPartnerId,
        IReadOnlyList<Partner> partners,
        IAppDbContext? persistViaDb)
    {
        if (expense.Status != ExpenseStatus.Paid)
        {
            expense.PaidByPartnerId = null;
            return;
        }

        var partnerIds = partners.Select(p => p.Id).ToHashSet();
        List<(Guid PartnerId, decimal Amount)> lines;

        if (payments is not null && payments.Any(p => p.Amount > 0m))
        {
            lines = payments
                .Where(p => p.Amount > 0m && partnerIds.Contains(p.PartnerId))
                .GroupBy(p => p.PartnerId)
                .Select(g => (g.Key, decimal.Round(g.Sum(x => x.Amount), 2)))
                .ToList();
        }
        else if (paidByPartnerId is Guid payerId)
        {
            if (!partnerIds.Contains(payerId))
            {
                throw new ValidationException("Ödeyen ortak bu plana ait değil.");
            }

            lines = [(payerId, expense.TotalAmount)];
        }
        else
        {
            throw new ValidationException("Ödendi durumunda en az bir ödeyen zorunludur.");
        }

        if (lines.Count == 0)
        {
            throw new ValidationException("Ödeyen ortak bu plana ait değil.");
        }

        var sum = lines.Sum(l => l.Amount);
        if (Math.Abs(sum - expense.TotalAmount) > 0.01m)
        {
            throw new ValidationException("Ödeyen tutarlarının toplamı gider tutarına eşit olmalıdır.");
        }

        foreach (var (partnerId, amount) in lines)
        {
            var row = new ExpensePayment
            {
                Id = Guid.NewGuid(),
                ExpenseId = expense.Id,
                PartnerId = partnerId,
                Amount = amount,
            };
            if (persistViaDb is not null)
            {
                persistViaDb.ExpensePayments.Add(row);
            }

            expense.Payments.Add(row);
        }

        expense.PaidByPartnerId = lines.Count == 1 ? lines[0].PartnerId : null;
    }
}

internal static class ExpenseMappings
{
    public static ExpenseDto ToDto(Expense e, IReadOnlyList<Partner> partners, bool canManage = false)
    {
        var lines = partners
            .Select(p => new ExpenseShareLineDto(
                p.Id,
                p.Name,
                ExpenseShareCalculator.GetPartnerShare(e, p, partners)))
            .ToList();

        var payments = e.Payments
            .Where(p => p.Amount > 0m)
            .Select(p => new ExpensePaymentDto(p.PartnerId, p.Amount))
            .ToList();

        if (payments.Count == 0 && e.PaidByPartnerId is Guid legacyPayer)
        {
            payments.Add(new ExpensePaymentDto(legacyPayer, e.TotalAmount));
        }

        var paidBy = payments.Count == 1 ? payments[0].PartnerId : e.PaidByPartnerId;

        return new ExpenseDto(
            e.Id,
            e.PlanId,
            e.CategoryId,
            e.Category?.Name,
            e.RecurrenceId,
            e.Name,
            e.OccurredOn,
            e.TotalAmount,
            e.ShareType,
            e.Status,
            paidBy,
            e.Note,
            e.CustomShares.Select(s => new CustomShareDto(s.PartnerId, s.Amount)).ToList(),
            lines,
            payments,
            canManage);
    }

    public static ExpenseRecurrenceDto ToDto(ExpenseRecurrence r) => new(
        r.Id,
        r.PlanId,
        r.CategoryId,
        r.Name,
        r.TotalAmount,
        r.ShareType,
        r.DefaultPaidByPartnerId,
        r.Frequency,
        r.AnchorDay,
        r.StartDate,
        r.EndDate,
        r.NextOccurrence,
        r.IsActive,
        r.Note,
        r.CustomShares.Select(s => new CustomShareDto(s.PartnerId, s.Amount)).ToList());
}

/// <summary>
/// Generates expenses from due recurrence definitions. This is intentionally invoked by a
/// scheduled background job, never by a read request.
/// </summary>
public static class ExpenseRecurrenceGenerator
{
    public static async Task GenerateAllDueAsync(IAppDbContext db, DateOnly today, CancellationToken ct)
    {
        var planIds = await db.ExpenseRecurrences.AsNoTracking()
            .Where(r => r.IsActive && !r.IsDeleted && r.NextOccurrence <= today)
            .Select(r => r.PlanId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var planId in planIds)
        {
            await GenerateDueAsync(db, planId, today, ct);
        }
    }

    public static async Task GenerateDueAsync(
        IAppDbContext db,
        Guid planId,
        DateOnly today,
        CancellationToken ct)
    {
        var recurrences = await db.ExpenseRecurrences
            .Include(r => r.CustomShares)
            .Where(r => r.PlanId == planId && r.IsActive && !r.IsDeleted && r.NextOccurrence <= today)
            .ToListAsync(ct);

        foreach (var recurrence in recurrences)
        {
            var guard = 0;
            while (recurrence.NextOccurrence <= today && guard++ < 36)
            {
                if (recurrence.EndDate is DateOnly end && recurrence.NextOccurrence > end)
                {
                    recurrence.IsActive = false;
                    break;
                }

                var period = ExpenseRecurrenceCalendar.PeriodKey(recurrence.Frequency, recurrence.NextOccurrence);
                var exists = await db.Expenses.AnyAsync(
                    e => e.RecurrenceId == recurrence.Id && e.PeriodKey == period && !e.IsDeleted, ct);
                if (!exists)
                {
                    var expense = new Expense
                    {
                        PlanId = planId,
                        CategoryId = recurrence.CategoryId,
                        RecurrenceId = recurrence.Id,
                        Name = recurrence.Name,
                        OccurredOn = recurrence.NextOccurrence,
                        TotalAmount = recurrence.TotalAmount,
                        ShareType = recurrence.ShareType,
                        Status = recurrence.DefaultPaidByPartnerId is null
                            ? ExpenseStatus.Planned
                            : ExpenseStatus.Paid,
                        PaidByPartnerId = recurrence.DefaultPaidByPartnerId,
                        Note = recurrence.Note,
                        PeriodKey = period,
                    };
                    foreach (var s in recurrence.CustomShares)
                    {
                        expense.CustomShares.Add(new ExpenseShare
                        {
                            ExpenseId = expense.Id,
                            PartnerId = s.PartnerId,
                            Amount = s.Amount,
                        });
                    }

                    if (recurrence.DefaultPaidByPartnerId is Guid defaultPayer)
                    {
                        expense.Payments.Add(new ExpensePayment
                        {
                            ExpenseId = expense.Id,
                            PartnerId = defaultPayer,
                            Amount = recurrence.TotalAmount,
                        });
                    }

                    db.Expenses.Add(expense);
                }

                recurrence.NextOccurrence = ExpenseRecurrenceCalendar.NextAfter(
                    recurrence.Frequency,
                    recurrence.AnchorDay,
                    recurrence.NextOccurrence);
            }
        }

        if (recurrences.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }
    }
}
