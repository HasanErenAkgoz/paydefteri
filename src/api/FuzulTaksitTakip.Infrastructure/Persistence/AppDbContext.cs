using FuzulTaksitTakip.Application.Common.Interfaces;
using FuzulTaksitTakip.Domain.Common;
using FuzulTaksitTakip.Domain.Entities;
using FuzulTaksitTakip.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FuzulTaksitTakip.Infrastructure.Persistence;

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
    public DbSet<PlanMember> PlanMembers => Set<PlanMember>();
    public DbSet<PlanInvite> PlanInvites => Set<PlanInvite>();
    public DbSet<PaymentReminderLog> PaymentReminderLogs => Set<PaymentReminderLog>();
    public DbSet<PlanActivityLog> PlanActivityLogs => Set<PlanActivityLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
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

        return base.SaveChangesAsync(cancellationToken);
    }
}
