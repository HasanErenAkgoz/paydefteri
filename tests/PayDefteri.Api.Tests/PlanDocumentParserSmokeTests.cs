using PayDefteri.Infrastructure.Documents;

namespace PayDefteri.Api.Tests;

public class PlanDocumentParserSmokeTests
{
    [Fact]
    public async Task Parses_user_fuzul_pdf_when_present()
    {
        var path = "/Users/yusuf/Downloads/Report1 (68) (1).pdf";
        if (!File.Exists(path))
        {
            return; // skip on CI / other machines
        }

        await using var stream = File.OpenRead(path);
        var parser = new PlanDocumentParser();
        var result = await parser.ParseAsync(stream, Path.GetFileName(path), "application/pdf");

        result.Installments.Should().HaveCountGreaterThanOrEqualTo(15);
        result.Installments.Sum(i => i.TotalAmount).Should().Be(1_070_000m);
        result.Title.Should().NotBeNullOrWhiteSpace();
        result.DeliveryIndex.Should().BeGreaterThanOrEqualTo(0);
        result.Installments[result.DeliveryIndex].Name.Should().Contain("Tahsisat");
    }
}
