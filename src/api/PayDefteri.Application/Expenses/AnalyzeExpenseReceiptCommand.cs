using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using PayDefteri.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace PayDefteri.Application.Expenses;

public sealed record ExpenseReceiptDraftDto(
    string? Name,
    decimal? TotalAmount,
    DateOnly? OccurredOn,
    Guid? CategoryId,
    string? CategoryName,
    int? InstallmentCount,
    string? DocumentNumber,
    string? Note,
    decimal Confidence,
    IReadOnlyList<string> LowConfidenceFields,
    IReadOnlyList<string> Warnings);

public sealed record AnalyzeExpenseReceiptCommand(
    Guid PlanId,
    string ContentType,
    byte[] Content) : IRequest<ExpenseReceiptDraftDto>;

public sealed class AnalyzeExpenseReceiptCommandValidator : AbstractValidator<AnalyzeExpenseReceiptCommand>
{
    private const int MaxFileSize = 8 * 1024 * 1024;
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];

    public AnalyzeExpenseReceiptCommandValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Fiş veya fatura görseli zorunludur.")
            .Must(content => content.Length <= MaxFileSize).WithMessage("Görsel en fazla 8 MB olabilir.");
        RuleFor(x => x.ContentType)
            .Must(contentType => AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Yalnızca JPEG, PNG veya WebP görseller desteklenir.");
        RuleFor(x => x)
            .Must(HasValidImageSignature)
            .WithMessage("Dosya içeriği geçerli bir JPEG, PNG veya WebP görseli değil.");
    }

    private static bool HasValidImageSignature(AnalyzeExpenseReceiptCommand command)
    {
        var content = command.Content;
        if (command.ContentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF;
        }
        if (command.ContentType.Equals("image/png", StringComparison.OrdinalIgnoreCase))
        {
            return content.Length >= 8 && content.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        }
        return content.Length >= 12
            && Encoding.ASCII.GetString(content, 0, 4) == "RIFF"
            && Encoding.ASCII.GetString(content, 8, 4) == "WEBP";
    }
}

public sealed class AnalyzeExpenseReceiptCommandHandler
    : IRequestHandler<AnalyzeExpenseReceiptCommand, ExpenseReceiptDraftDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _authorization;
    private readonly ICurrentUser _currentUser;
    private readonly IExpenseReceiptAnalyzer _analyzer;

    public AnalyzeExpenseReceiptCommandHandler(
        IAppDbContext db,
        IPlanAuthorization authorization,
        ICurrentUser currentUser,
        IExpenseReceiptAnalyzer analyzer)
    {
        _db = db;
        _authorization = authorization;
        _currentUser = currentUser;
        _analyzer = analyzer;
    }

    public async Task<ExpenseReceiptDraftDto> Handle(
        AnalyzeExpenseReceiptCommand request,
        CancellationToken cancellationToken)
    {
        await ExpensePlanGuards.EnsureExpensePlanAsync(
            _db,
            _authorization,
            request.PlanId,
            cancellationToken);

        var categories = await _db.ExpenseCategories.AsNoTracking()
            .Where(category => category.PlanId == request.PlanId && !category.IsDeleted)
            .OrderBy(category => category.SortOrder)
            .Select(category => new { category.Id, category.Name })
            .ToListAsync(cancellationToken);

        var safetyIdentifier = CreateSafetyIdentifier(_currentUser.UserId);
        var result = await _analyzer.AnalyzeAsync(
            new ExpenseReceiptAnalysisInput(
                request.ContentType,
                request.Content,
                categories.Select(category => category.Name).ToArray(),
                safetyIdentifier),
            cancellationToken);

        var category = categories.FirstOrDefault(item =>
            !string.IsNullOrWhiteSpace(result.CategoryName)
            && string.Equals(item.Name, result.CategoryName, StringComparison.OrdinalIgnoreCase));

        return new ExpenseReceiptDraftDto(
            result.MerchantName,
            result.TotalAmount,
            result.OccurredOn,
            category?.Id,
            category?.Name,
            result.InstallmentCount is >= 2 and <= 120 ? result.InstallmentCount : null,
            result.DocumentNumber,
            result.Note,
            Math.Clamp(result.Confidence, 0, 1),
            result.LowConfidenceFields,
            result.Warnings);
    }

    private static string CreateSafetyIdentifier(string? userId)
    {
        var value = string.IsNullOrWhiteSpace(userId) ? "anonymous" : userId;
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }
}
