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
        builder.HasIndex(x => x.OwnerUserId);

        builder.HasMany(x => x.Partners)
            .WithOne(x => x.Plan)
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Installments)
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
        builder.HasIndex(x => x.PlanId);
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
