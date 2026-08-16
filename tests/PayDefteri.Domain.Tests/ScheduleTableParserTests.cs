using FluentAssertions;
using PayDefteri.Domain.Services;

namespace PayDefteri.Domain.Tests;

public class ScheduleTableParserTests
{
    private const string FuzulSample = """
Belge No: 12505584
05.05.2026
Konut
BURAK BEY REF (7403942),
Tahsisat Dönemi : 8 Tahsisat Dönemi : 6
1 Organizasyon Ücreti Peşinat 05.05.2026 35,000.00 ₺
Ay Taksit Bilgileri Taksit Tarihi Taksit Tutarı
1 Peşinat 05.05.2026 250,000.00 ₺
1 1. Tasarruf Taksiti 05.05.2026 25,000.00 ₺
2 Organizasyon Ücreti 1. Taksiti 05.06.2026 8,750.00 ₺
2 2. Tasarruf Taksiti 05.06.2026 25,000.00 ₺
6 6. Tahsisat 05.10.2026 25,000.00 ₺
20 20. Finansman Taksiti 05.12.2027 102,000.00 ₺
""";

    [Fact]
    public void Fuzul_sample_merges_same_day_and_finds_delivery()
    {
        var (title, description, deliveryIndex, rows, warnings) =
            ScheduleTableParser.ParseFromPlainText(FuzulSample);

        title.Should().Contain("BURAK");
        description.Should().Contain("12505584");
        warnings.Should().BeEmpty();
        rows.Should().HaveCountGreaterThanOrEqualTo(4);

        var first = rows[0];
        first.Due.Should().Be(new DateOnly(2026, 5, 5));
        first.Amount.Should().Be(310_000m); // 35k + 250k + 25k

        rows.Should().Contain(r => r.Name.Contains("Tahsisat"));
        deliveryIndex.Should().BeGreaterThanOrEqualTo(0);
        rows[deliveryIndex].Name.Should().Contain("Tahsisat");
    }

    [Theory]
    [InlineData("1.070.000,00", 1_070_000)]
    [InlineData("250,000.00", 250_000)]
    [InlineData("8750.00", 8750)]
    public void Money_parsing_handles_tr_and_en(string raw, decimal expected)
    {
        ScheduleTableParser.TryParseTrMoney(raw, out var amount).Should().BeTrue();
        amount.Should().Be(expected);
    }
}
