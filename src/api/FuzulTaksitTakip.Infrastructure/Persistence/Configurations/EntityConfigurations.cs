using FuzulTaksitTakip.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FuzulTaksitTakip.Infrastructure.Persistence.Configurations;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("plans");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OwnerUserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.IbanMode).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.SettlementIban).HasMaxLength(34);
        builder.Property(x => x.ReminderDaysBefore).HasColumnType("integer[]");
        builder.Property(x => x.ReminderDaysAfter).HasColumnType("integer[]");
        builder.HasIndex(x => x.OwnerUserId);

        builder.HasMany(x => x.Partners)
            .WithOne(x => x.Plan)
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Installments)
            .WithOne(x => x.Plan)
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Members)
            .WithOne(x => x.Plan)
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Invites)
            .WithOne(x => x.Plan)
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.ReminderLogs)
            .WithOne(x => x.Plan)
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.ActivityLogs)
            .WithOne(x => x.Plan)
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PartnerConfiguration : IEntityTypeConfiguration<Partner>
{
    public void Configure(EntityTypeBuilder<Partner> builder)
    {
        builder.ToTable("partners");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Color).HasMaxLength(32).IsRequired();
        builder.Property(x => x.DefaultPct).HasPrecision(18, 2);
        builder.Property(x => x.LinkedUserId).HasMaxLength(450);
        builder.Property(x => x.Iban).HasMaxLength(34);
        builder.HasIndex(x => x.PlanId);
        builder.HasIndex(x => new { x.PlanId, x.LinkedUserId });
    }
}

public class InstallmentConfiguration : IEntityTypeConfiguration<Installment>
{
    public void Configure(EntityTypeBuilder<Installment> builder)
    {
        builder.ToTable("installments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.ShareType).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(x => x.PlanId);

        builder.HasMany(x => x.CustomShares)
            .WithOne(x => x.Installment)
            .HasForeignKey(x => x.InstallmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Payments)
            .WithOne(x => x.Installment)
            .HasForeignKey(x => x.InstallmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class InstallmentShareConfiguration : IEntityTypeConfiguration<InstallmentShare>
{
    public void Configure(EntityTypeBuilder<InstallmentShare> builder)
    {
        builder.ToTable("installment_shares");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.InstallmentId, x.PartnerId }).IsUnique();

        builder.HasOne(x => x.Partner)
            .WithMany()
            .HasForeignKey(x => x.PartnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Note).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ReceiptStorageKey).HasMaxLength(500);
        builder.Property(x => x.ReceiptContentType).HasMaxLength(128);
        builder.Property(x => x.ReceiptFileName).HasMaxLength(260);
        builder.Property(x => x.ReviewStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.ReviewedByUserId).HasMaxLength(450);
        builder.HasIndex(x => new { x.InstallmentId, x.PartnerId }).IsUnique();

        builder.HasOne(x => x.Partner)
            .WithMany()
            .HasForeignKey(x => x.PartnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PaidByPartner)
            .WithMany()
            .HasForeignKey(x => x.PaidByPartnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PlanMemberConfiguration : IEntityTypeConfiguration<PlanMember>
{
    public void Configure(EntityTypeBuilder<PlanMember> builder)
    {
        builder.ToTable("plan_members");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(x => new { x.PlanId, x.UserId }).IsUnique();

        builder.HasOne(x => x.Partner)
            .WithMany()
            .HasForeignKey(x => x.PartnerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class PlanInviteConfiguration : IEntityTypeConfiguration<PlanInvite>
{
    public void Configure(EntityTypeBuilder<PlanInvite> builder)
    {
        builder.ToTable("plan_invites");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Token).HasMaxLength(64).IsRequired();
        builder.Property(x => x.InvitedByUserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(x => x.Token).IsUnique();
        builder.HasIndex(x => new { x.PlanId, x.Email, x.Status });

        builder.HasOne(x => x.Partner)
            .WithMany()
            .HasForeignKey(x => x.PartnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PaymentReminderLogConfiguration : IEntityTypeConfiguration<PaymentReminderLog>
{
    public void Configure(EntityTypeBuilder<PaymentReminderLog> builder)
    {
        builder.ToTable("payment_reminder_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(16);
        builder.HasIndex(x => new
            {
                x.PlanId,
                x.InstallmentId,
                x.PartnerId,
                x.Kind,
                x.OffsetDays,
                x.SentOn
            })
            .IsUnique();

        builder.HasOne(x => x.Installment)
            .WithMany()
            .HasForeignKey(x => x.InstallmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PlanActivityLogConfiguration : IEntityTypeConfiguration<PlanActivityLog>
{
    public void Configure(EntityTypeBuilder<PlanActivityLog> builder)
    {
        builder.ToTable("plan_activity_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ActorUserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.ActorDisplayName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(48);
        builder.Property(x => x.Message).HasMaxLength(1000).IsRequired();
        builder.HasIndex(x => new { x.PlanId, x.CreatedAtUtc });
    }
}
