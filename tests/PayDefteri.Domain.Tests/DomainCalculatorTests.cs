using FluentAssertions;
using PayDefteri.Domain.Entities;
using PayDefteri.Domain.Enums;
using PayDefteri.Domain.Services;
using PayDefteri.Domain.Templates;

namespace PayDefteri.Domain.Tests;

public class ShareCalculatorTests
{
    [Fact]
    public void Default_fifty_fifty_on_25000_yields_12500_each()
    {
        var a = new Partner { Id = Guid.NewGuid(), DefaultPct = 50m };
        var b = new Partner { Id = Guid.NewGuid(), DefaultPct = 50m };
        var inst = new Installment { TotalAmount = 25_000m, ShareType = ShareType.Default };

        ShareCalculator.GetPartnerShare(inst, a, new[] { a, b }).Should().Be(12_500m);
        ShareCalculator.GetPartnerShare(inst, b, new[] { a, b }).Should().Be(12_500m);
    }

    [Fact]
    public void Equal_three_partners_on_30000_yields_10000_each()
    {
        var partners = Enumerable.Range(0, 3)
            .Select(_ => new Partner { Id = Guid.NewGuid(), DefaultPct = 0 })
            .ToList();
        var inst = new Installment { TotalAmount = 30_000m, ShareType = ShareType.Equal };

        foreach (var p in partners)
        {
            ShareCalculator.GetPartnerShare(inst, p, partners).Should().Be(10_000m);
        }
    }

    [Fact]
    public void Custom_shares_must_match_total()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var inst = new Installment
        {
            TotalAmount = 100m,
            ShareType = ShareType.Custom,
            CustomShares =
            {
                new InstallmentShare { PartnerId = a, Amount = 60m },
                new InstallmentShare { PartnerId = b, Amount = 30m }
            }
        };

        ShareCalculator.CustomSharesMatchTotal(inst).Should().BeFalse();
        inst.CustomShares.Last().Amount = 40m;
        ShareCalculator.CustomSharesMatchTotal(inst).Should().BeTrue();
    }
}

public class InstallmentStatusCalculatorTests
{
    [Theory]
    [InlineData(0, InstallmentStatus.Pending)]
    [InlineData(1, InstallmentStatus.Partial)]
    [InlineData(2, InstallmentStatus.Full)]
    public void Status_from_paid_count(int paidCount, InstallmentStatus expected)
    {
        var inst = new Installment();
        var partnerIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        for (var i = 0; i < paidCount; i++)
        {
            inst.Payments.Add(new Payment { PartnerId = partnerIds[i], IsPaid = true });
        }

        InstallmentStatusCalculator.Calculate(inst, 2).Should().Be(expected);
    }
}

public class SettlementCalculatorTests
{
    [Fact]
    public void Pay_on_behalf_credits_payer_debits_owner()
    {
        var a = new Partner { Id = Guid.NewGuid(), DefaultPct = 50m };
        var b = new Partner { Id = Guid.NewGuid(), DefaultPct = 50m };
        var inst = new Installment
        {
            TotalAmount = 20_000m,
            ShareType = ShareType.Default,
            Payments =
            {
                new Payment { PartnerId = b.Id, IsPaid = true, PaidByPartnerId = a.Id }
            }
        };

        var balances = SettlementCalculator.ComputeBalances(new[] { inst }, new[] { a, b });
        balances[a.Id].Should().Be(10_000m);
        balances[b.Id].Should().Be(-10_000m);
    }
}

public class FuzulSeedTemplateTests
{
    [Fact]
    public void Fuzul_seed_grand_total_is_1070000()
    {
        var (_, _, installments) = FuzulSeedTemplate.Create("user");
        installments.Sum(i => i.TotalAmount).Should().Be(FuzulSeedTemplate.ExpectedGrandTotal);
        installments.Should().HaveCount(20);
    }
}

public class PlanTemplateCatalogTests
{
    [Theory]
    [InlineData("fuzul", 20, 1_070_000)]
    [InlineData("eminevim", 12, 680_000)]
    [InlineData("birevim", 12, 289_996)]
    [InlineData("katilimevim", 12, 925_000)]
    [InlineData("sinpas", 12, 1_230_000)]
    [InlineData("empty", 0, 0)]
    public void Template_totals_and_counts_match_html(string key, int count, decimal total)
    {
        var def = PlanTemplateCatalog.Get(key);
        def.Installments.Should().HaveCount(count);
        def.Installments.Sum(i => i.TotalAmount).Should().Be(total);

        var preview = PlanTemplateCatalog.ToPreview(def);
        preview.GrandTotal.Should().Be(total);
        preview.InstallmentCount.Should().Be(count);
    }

    [Fact]
    public void Negative_Unknown_template_key_throws()
    {
        var act = () => PlanTemplateCatalog.Get("does-not-exist");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Negative_Custom_shares_mismatch_is_rejected_by_calculator()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var inst = new Installment
        {
            TotalAmount = 25_000m,
            ShareType = ShareType.Custom,
            CustomShares =
            {
                new InstallmentShare { PartnerId = a, Amount = 10_000m },
                new InstallmentShare { PartnerId = b, Amount = 10_000m }
            }
        };

        ShareCalculator.CustomSharesMatchTotal(inst).Should().BeFalse();
    }
}
