using FuzulTaksitTakip.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FuzulTaksitTakip.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<Plan> Plans { get; }
    DbSet<Partner> Partners { get; }
    DbSet<Installment> Installments { get; }
    DbSet<InstallmentShare> InstallmentShares { get; }
    DbSet<Payment> Payments { get; }
    DbSet<ExpenseCategory> ExpenseCategories { get; }
    DbSet<Expense> Expenses { get; }
    DbSet<ExpenseShare> ExpenseShares { get; }
    DbSet<ExpensePayment> ExpensePayments { get; }
    DbSet<ExpenseRecurrence> ExpenseRecurrences { get; }
    DbSet<ExpenseShareTemplate> ExpenseShareTemplates { get; }
    DbSet<SettlementTransfer> SettlementTransfers { get; }
    DbSet<PlanMember> PlanMembers { get; }
    DbSet<PlanInvite> PlanInvites { get; }
    DbSet<PaymentReminderLog> PaymentReminderLogs { get; }
    DbSet<PlanActivityLog> PlanActivityLogs { get; }
    DbSet<MobileRefreshSession> MobileRefreshSessions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<bool> TryClaimMobileRefreshSessionAsync(
        Guid sessionId,
        DateTime claimedAtUtc,
        Guid replacementSessionId,
        CancellationToken cancellationToken = default);
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
}
