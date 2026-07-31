using FluentAssertions;
using FuzulTaksitTakip.Domain.Entities;
using FuzulTaksitTakip.Domain.Enums;
using FuzulTaksitTakip.Domain.Services;
using FuzulTaksitTakip.Domain.Templates;

namespace FuzulTaksitTakip.Domain.Tests;

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
