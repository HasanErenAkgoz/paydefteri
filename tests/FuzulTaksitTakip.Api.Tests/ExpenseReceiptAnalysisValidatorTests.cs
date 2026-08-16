using FluentAssertions;
using FuzulTaksitTakip.Application.Expenses;
using Xunit;

namespace FuzulTaksitTakip.Api.Tests;

public sealed class ExpenseReceiptAnalysisValidatorTests
{
    private readonly AnalyzeExpenseReceiptCommandValidator _validator = new();

    [Fact]
    public void Valid_jpeg_is_accepted()
    {
        var command = new AnalyzeExpenseReceiptCommand(
            Guid.NewGuid(),
            "image/jpeg",
            [0xFF, 0xD8, 0xFF, 0xE0]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Spoofed_image_content_is_rejected()
    {
        var command = new AnalyzeExpenseReceiptCommand(
            Guid.NewGuid(),
            "image/png",
            "this is not an image"u8.ToArray());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorMessage.Contains("geçerli bir JPEG, PNG veya WebP", StringComparison.Ordinal));
    }

    [Fact]
    public void Image_larger_than_eight_megabytes_is_rejected()
    {
        var content = new byte[8 * 1024 * 1024 + 1];
        content[0] = 0xFF;
        content[1] = 0xD8;
        content[2] = 0xFF;
        var command = new AnalyzeExpenseReceiptCommand(Guid.NewGuid(), "image/jpeg", content);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage.Contains("8 MB", StringComparison.Ordinal));
    }
}
