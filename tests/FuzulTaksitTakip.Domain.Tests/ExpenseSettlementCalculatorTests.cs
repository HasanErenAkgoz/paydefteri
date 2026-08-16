using FluentAssertions;
using FuzulTaksitTakip.Domain.Entities;
using FuzulTaksitTakip.Domain.Enums;
using FuzulTaksitTakip.Domain.Services;

namespace FuzulTaksitTakip.Domain.Tests;

public class ExpenseSettlementCalculatorTests
{
    [Fact]
    public void Equal_split_paid_by_one_partner_creates_half_debt()
    {
        var eren = new Partner { Id = Guid.NewGuid(), Name = "Eren", DefaultPct = 50 };
        var yusuf = new Partner { Id = Guid.NewGuid(), Name = "Yusuf", DefaultPct = 50 };
        var partners = new List<Partner> { eren, yusuf };

        var expense = new Expense
        {
            Name = "Elektrik",
            TotalAmount = 1200m,
            ShareType = ShareType.Equal,
            Status = ExpenseStatus.Paid,
            PaidByPartnerId = eren.Id,
        };

        var balances = ExpenseSettlementCalculator.ComputeBalances(
            new[] { expense },
            Array.Empty<SettlementTransfer>(),
            partners);

        balances[eren.Id].Should().Be(600m);
        balances[yusuf.Id].Should().Be(-600m);
    }

    [Fact]
    public void Full_custom_share_to_payer_creates_no_debt()
    {
        var eren = new Partner { Id = Guid.NewGuid(), Name = "Eren", DefaultPct = 50 };
        var yusuf = new Partner { Id = Guid.NewGuid(), Name = "Yusuf", DefaultPct = 50 };
        var partners = new List<Partner> { eren, yusuf };

        var expense = new Expense
        {
            Name = "Kişisel",
            TotalAmount = 500m,
            ShareType = ShareType.Custom,
            Status = ExpenseStatus.Paid,
            PaidByPartnerId = eren.Id,
            CustomShares =
            {
                new ExpenseShare { PartnerId = eren.Id, Amount = 500m },
                new ExpenseShare { PartnerId = yusuf.Id, Amount = 0m },
            },
        };

        var balances = ExpenseSettlementCalculator.ComputeBalances(
            new[] { expense },
            Array.Empty<SettlementTransfer>(),
            partners);

        balances[eren.Id].Should().Be(0m);
        balances[yusuf.Id].Should().Be(0m);
    }

    [Fact]
    public void Transfer_clears_debt()
    {
        var eren = new Partner { Id = Guid.NewGuid(), Name = "Eren", DefaultPct = 50 };
        var yusuf = new Partner { Id = Guid.NewGuid(), Name = "Yusuf", DefaultPct = 50 };
        var partners = new List<Partner> { eren, yusuf };

        var expense = new Expense
        {
            TotalAmount = 1200m,
            ShareType = ShareType.Equal,
            Status = ExpenseStatus.Paid,
            PaidByPartnerId = eren.Id,
        };

        var transfer = new SettlementTransfer
        {
            FromPartnerId = yusuf.Id,
            ToPartnerId = eren.Id,
            Amount = 600m,
        };

        var balances = ExpenseSettlementCalculator.ComputeBalances(
            new[] { expense },
            new[] { transfer },
            partners);

        balances[eren.Id].Should().Be(0m);
        balances[yusuf.Id].Should().Be(0m);
    }

    [Fact]
    public void Multi_payer_equal_share_nets_difference_only()
    {
        // Market 300, equal shares (150/150). Ayşe paid 200, Mehmet paid 100 → Mehmet owes 50.
        var ayse = new Partner { Id = Guid.NewGuid(), Name = "Ayşe", DefaultPct = 50 };
        var mehmet = new Partner { Id = Guid.NewGuid(), Name = "Mehmet", DefaultPct = 50 };
        var partners = new List<Partner> { ayse, mehmet };

        var expense = new Expense
        {
            Name = "Market",
            TotalAmount = 300m,
            ShareType = ShareType.Equal,
            Status = ExpenseStatus.Paid,
            Payments =
            {
                new ExpensePayment { PartnerId = ayse.Id, Amount = 200m },
                new ExpensePayment { PartnerId = mehmet.Id, Amount = 100m },
            },
        };

        var balances = ExpenseSettlementCalculator.ComputeBalances(
            new[] { expense },
            Array.Empty<SettlementTransfer>(),
            partners);

        balances[ayse.Id].Should().Be(50m);
        balances[mehmet.Id].Should().Be(-50m);
    }

    [Fact]
    public void Multi_payer_matching_shares_creates_no_debt()
    {
        var ayse = new Partner { Id = Guid.NewGuid(), Name = "Ayşe", DefaultPct = 50 };
        var mehmet = new Partner { Id = Guid.NewGuid(), Name = "Mehmet", DefaultPct = 50 };
        var partners = new List<Partner> { ayse, mehmet };

        var expense = new Expense
        {
            TotalAmount = 300m,
            ShareType = ShareType.Equal,
            Status = ExpenseStatus.Paid,
            Payments =
            {
                new ExpensePayment { PartnerId = ayse.Id, Amount = 150m },
                new ExpensePayment { PartnerId = mehmet.Id, Amount = 150m },
            },
        };

        var balances = ExpenseSettlementCalculator.ComputeBalances(
            new[] { expense },
            Array.Empty<SettlementTransfer>(),
            partners);

        balances[ayse.Id].Should().Be(0m);
        balances[mehmet.Id].Should().Be(0m);
    }

    [Fact]
    public void Payments_collection_overrides_legacy_PaidByPartnerId()
    {
        var ayse = new Partner { Id = Guid.NewGuid(), Name = "Ayşe", DefaultPct = 50 };
        var mehmet = new Partner { Id = Guid.NewGuid(), Name = "Mehmet", DefaultPct = 50 };
        var partners = new List<Partner> { ayse, mehmet };

        var expense = new Expense
        {
            TotalAmount = 300m,
            ShareType = ShareType.Equal,
            Status = ExpenseStatus.Paid,
            PaidByPartnerId = ayse.Id, // legacy would imply Ayşe paid 300
            Payments =
            {
                new ExpensePayment { PartnerId = ayse.Id, Amount = 200m },
                new ExpensePayment { PartnerId = mehmet.Id, Amount = 100m },
            },
        };

        var balances = ExpenseSettlementCalculator.ComputeBalances(
            new[] { expense },
            Array.Empty<SettlementTransfer>(),
            partners);

        balances[ayse.Id].Should().Be(50m);
        balances[mehmet.Id].Should().Be(-50m);
    }
}
