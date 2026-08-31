using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayDefteri.Domain.Entities;

namespace PayDefteri.Infrastructure.Persistence.Configurations;

public sealed class SpendingStatementConfiguration : IEntityTypeConfiguration<SpendingStatement>
{
    public void Configure(EntityTypeBuilder<SpendingStatement> builder)
    {
        builder.ToTable("spending_statements");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OwnerUserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.SourceHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SourceFileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.SourceKind).HasMaxLength(16).IsRequired();
        builder.HasIndex(x => new { x.OwnerUserId, x.SourceHash }).IsUnique();
        builder.HasIndex(x => new { x.OwnerUserId, x.CreatedAtUtc });
        builder.HasMany(x => x.Transactions)
            .WithOne(x => x.Statement)
            .HasForeignKey(x => x.StatementId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SpendingMerchantPreferenceConfiguration : IEntityTypeConfiguration<SpendingMerchantPreference>
{
    public void Configure(EntityTypeBuilder<SpendingMerchantPreference> builder)
    {
        builder.ToTable("spending_merchant_preferences");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OwnerUserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.MerchantKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => new { x.OwnerUserId, x.MerchantKey }).IsUnique();
    }
}

public sealed class SpendingCategoryBudgetConfiguration : IEntityTypeConfiguration<SpendingCategoryBudget>
{
    public void Configure(EntityTypeBuilder<SpendingCategoryBudget> builder)
    {
        builder.ToTable("spending_category_budgets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OwnerUserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.OwnerUserId, x.Month, x.Category, x.Currency }).IsUnique();
    }
}

public sealed class SpendingTransactionConfiguration : IEntityTypeConfiguration<SpendingTransaction>
{
    public void Configure(EntityTypeBuilder<SpendingTransaction> builder)
    {
        builder.ToTable("spending_transactions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.MerchantName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => new { x.StatementId, x.OccurredOn });
    }
}
