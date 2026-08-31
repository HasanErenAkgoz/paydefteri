using PayDefteri.Application.Common.Interfaces;
using PayDefteri.Application.Common.Exceptions;
using PayDefteri.Domain.Common;
using PayDefteri.Domain.Entities;
using PayDefteri.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace PayDefteri.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<AppUser>, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<Installment> Installments => Set<Installment>();
    public DbSet<InstallmentShare> InstallmentShares => Set<InstallmentShare>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<ExpenseShare> ExpenseShares => Set<ExpenseShare>();
    public DbSet<ExpensePayment> ExpensePayments => Set<ExpensePayment>();
    public DbSet<ExpenseRecurrence> ExpenseRecurrences => Set<ExpenseRecurrence>();
    public DbSet<ExpenseShareTemplate> ExpenseShareTemplates => Set<ExpenseShareTemplate>();
    public DbSet<SettlementTransfer> SettlementTransfers => Set<SettlementTransfer>();
    public DbSet<PlanMember> PlanMembers => Set<PlanMember>();
    public DbSet<PlanInvite> PlanInvites => Set<PlanInvite>();
    public DbSet<PaymentReminderLog> PaymentReminderLogs => Set<PaymentReminderLog>();
    public DbSet<PlanActivityLog> PlanActivityLogs => Set<PlanActivityLog>();
    public DbSet<MobileRefreshSession> MobileRefreshSessions => Set<MobileRefreshSession>();
    public DbSet<SpendingStatement> SpendingStatements => Set<SpendingStatement>();
    public DbSet<SpendingTransaction> SpendingTransactions => Set<SpendingTransaction>();
    public DbSet<SpendingMerchantPreference> SpendingMerchantPreferences => Set<SpendingMerchantPreference>();
    public DbSet<SpendingCategoryBudget> SpendingCategoryBudgets => Set<SpendingCategoryBudget>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_spending_statements_OwnerUserId_SourceHash",
            })
        {
            throw new ConflictException("Bu ekstre daha önce yüklenmiş.");
        }
    }

    public async Task<bool> TryClaimMobileRefreshSessionAsync(
        Guid sessionId,
        DateTime claimedAtUtc,
        Guid replacementSessionId,
        CancellationToken cancellationToken = default)
    {
        var affected = await MobileRefreshSessions
            .Where(x => x.Id == sessionId
                && x.RevokedAtUtc == null
                && x.ExpiresAtUtc > claimedAtUtc)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.LastUsedAtUtc, claimedAtUtc)
                .SetProperty(x => x.RevokedAtUtc, claimedAtUtc)
                .SetProperty(x => x.ReplacedBySessionId, replacementSessionId), cancellationToken);
        return affected == 1;
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
