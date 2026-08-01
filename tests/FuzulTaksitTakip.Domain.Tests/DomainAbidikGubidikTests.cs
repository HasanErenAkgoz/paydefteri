using FluentAssertions;
using FuzulTaksitTakip.Domain.Entities;
using FuzulTaksitTakip.Domain.Enums;
using FuzulTaksitTakip.Domain.Services;

namespace FuzulTaksitTakip.Domain.Tests;

public class DomainAbidikGubidikTests
{
    [Fact]
    public void Equal_three_partners_on_100_uses_repeating_decimal()
    {
        var partners = Enumerable.Range(0, 3).Select(_ => new Partner { Id = Guid.NewGuid() }).ToList();
        var inst = new Installment { TotalAmount = 100m, ShareType = ShareType.Equal };

        var shares = partners.Select(p => ShareCalculator.GetPartnerShare(inst, p, partners)).ToList();
        shares.Should().OnlyContain(s => s == 100m / 3m);
        shares.Sum().Should().Be(100m);
    }

    [Fact]
    public void Custom_missing_partner_share_is_zero_not_exception()
    {
        var a = new Partner { Id = Guid.NewGuid() };
        var b = new Partner { Id = Guid.NewGuid() };
        var inst = new Installment
        {
            TotalAmount = 100m,
            ShareType = ShareType.Custom,
            CustomShares = { new InstallmentShare { PartnerId = a.Id, Amount = 100m } }
        };

        ShareCalculator.GetPartnerShare(inst, b, new[] { a, b }).Should().Be(0m);
    }

    [Theory]
    [InlineData(99.99, true)]
    [InlineData(100.01, true)]
    [InlineData(99.98, false)]
    [InlineData(100.02, false)]
    public void Custom_tolerance_is_one_kurus(decimal sum, bool expected)
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var first = Math.Round(sum / 2m, 2);
        var second = sum - first;
        var inst = new Installment
        {
            TotalAmount = 100m,
            ShareType = ShareType.Custom,
            CustomShares =
            {
                new InstallmentShare { PartnerId = a, Amount = first },
                new InstallmentShare { PartnerId = b, Amount = second }
            }
        };

        ShareCalculator.CustomSharesMatchTotal(inst).Should().Be(expected);
    }

    [Fact]
    public void Default_pct_within_tolerance_sums_to_100()
    {
        var partners = new[]
        {
            new Partner { DefaultPct = 50.005m },
            new Partner { DefaultPct = 49.995m }
        };
        ShareCalculator.DefaultPercentagesSumTo100(partners).Should().BeTrue();
    }

    [Fact]
    public void Equal_with_zero_partners_divides_by_one()
    {
        var ghost = new Partner { Id = Guid.NewGuid() };
        var inst = new Installment { TotalAmount = 42m, ShareType = ShareType.Equal };
        ShareCalculator.GetPartnerShare(inst, ghost, Array.Empty<Partner>()).Should().Be(42m);
    }

    [Fact]
    public void Status_with_zero_partners_count_is_pending()
    {
        var inst = new Installment();
        inst.Payments.Add(new Payment { PartnerId = Guid.NewGuid(), IsPaid = true });
        InstallmentStatusCalculator.Calculate(inst, 0).Should().Be(InstallmentStatus.Pending);
    }

    [Fact]
    public void Status_more_paid_than_partners_is_still_full()
    {
        var inst = new Installment();
        for (var i = 0; i < 5; i++)
        {
            inst.Payments.Add(new Payment { PartnerId = Guid.NewGuid(), IsPaid = true });
        }

        InstallmentStatusCalculator.Calculate(inst, 2).Should().Be(InstallmentStatus.Full);
    }

    [Fact]
    public void Settlement_self_pay_and_null_payer_do_not_move_balances()
    {
        var a = new Partner { Id = Guid.NewGuid(), DefaultPct = 50 };
        var b = new Partner { Id = Guid.NewGuid(), DefaultPct = 50 };
        var inst = new Installment
        {
            TotalAmount = 100m,
            ShareType = ShareType.Default,
            Payments =
            {
                new Payment { PartnerId = a.Id, IsPaid = true, PaidByPartnerId = a.Id },
                new Payment { PartnerId = b.Id, IsPaid = true, PaidByPartnerId = null }
            }
        };

        var balances = SettlementCalculator.ComputeBalances(new[] { inst }, new[] { a, b });
        balances[a.Id].Should().Be(0m);
        balances[b.Id].Should().Be(0m);
    }

    [Fact]
    public void Settlement_three_party_cycle_nets_to_zero()
    {
        var a = new Partner { Id = Guid.NewGuid() };
        var b = new Partner { Id = Guid.NewGuid() };
        var c = new Partner { Id = Guid.NewGuid() };
        var partners = new[] { a, b, c };
        var inst = new Installment
        {
            TotalAmount = 90m,
            ShareType = ShareType.Equal,
            Payments =
            {
                new Payment { PartnerId = a.Id, IsPaid = true, PaidByPartnerId = b.Id },
                new Payment { PartnerId = b.Id, IsPaid = true, PaidByPartnerId = c.Id },
                new Payment { PartnerId = c.Id, IsPaid = true, PaidByPartnerId = a.Id }
            }
        };

        var balances = SettlementCalculator.ComputeBalances(new[] { inst }, partners);
        balances.Values.Sum().Should().Be(0m);
        balances.Values.Should().OnlyContain(v => v == 0m);
    }

    [Fact]
    public void Settlement_unknown_payer_still_credits_ghost_key()
    {
        var a = new Partner { Id = Guid.NewGuid(), DefaultPct = 100 };
        var ghostPayer = Guid.NewGuid();
        var inst = new Installment
        {
            TotalAmount = 50m,
            ShareType = ShareType.Default,
            Payments =
            {
                new Payment { PartnerId = a.Id, IsPaid = true, PaidByPartnerId = ghostPayer }
            }
        };

        var balances = SettlementCalculator.ComputeBalances(new[] { inst }, new[] { a });
        balances[a.Id].Should().Be(-50m);
        balances[ghostPayer].Should().Be(50m);
    }

    [Theory]
    [InlineData("tr33 0006 1005 1978 6457 8413 26", true, "TR330006100519786457841326")]
    [InlineData("TR33-0006-1005-1978-6457-8413-26", true, "TR330006100519786457841326")]
    [InlineData("TR123", false, "TR123")]
    [InlineData("DE89370400440532013000", false, "DE89370400440532013000")]
    [InlineData("   ", false, null)]
    [InlineData(null, false, null)]
    public void Iban_normalizer_weird_inputs(string? input, bool valid, string? normalized)
    {
        IbanNormalizer.IsValidTurkishIban(input).Should().Be(valid);
        IbanNormalizer.Normalize(input).Should().Be(normalized);
    }

    [Fact]
    public void Iban_accepts_format_only_without_mod97_check()
    {
        // 24 digits after TR but nonsense checksum — still valid by current rules
        var nonsense = "TR000000000000000000000000";
        IbanNormalizer.IsValidTurkishIban(nonsense).Should().BeTrue();
    }
}
