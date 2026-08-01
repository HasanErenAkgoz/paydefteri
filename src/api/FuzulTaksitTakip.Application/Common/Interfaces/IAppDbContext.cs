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
    DbSet<PlanMember> PlanMembers { get; }
    DbSet<PlanInvite> PlanInvites { get; }
    DbSet<PaymentReminderLog> PaymentReminderLogs { get; }
    DbSet<PlanActivityLog> PlanActivityLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
