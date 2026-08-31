using System.Security.Cryptography;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PayDefteri.Application.Common;
using PayDefteri.Application.Common.Exceptions;
using PayDefteri.Application.Common.Interfaces;
using PayDefteri.Domain.Entities;
using PayDefteri.Domain.Templates;

namespace PayDefteri.Application.SpendingAnalysis;

public sealed record SpendingCurrencyTotalsDto(
    string Currency,
    decimal Spending,
    decimal Refunds,
    decimal Net);

public sealed record SpendingStatementSummaryDto(
    Guid Id,
    string SourceFileName,
    string SourceKind,
    DateOnly? PeriodStart,
    DateOnly? PeriodEnd,
    int TransactionCount,
    IReadOnlyList<SpendingCurrencyTotalsDto> Totals,
    DateTime CreatedAtUtc);

public sealed record SpendingTransactionDto(
    Guid Id,
    DateOnly OccurredOn,
    string Description,
    string MerchantName,
    decimal Amount,
    bool IsRefund,
    string Currency,
    string Category,
    int? InstallmentCurrent,
    int? InstallmentTotal);

public sealed record SpendingStatementDetailDto(
    Guid Id,
    string SourceFileName,
    string SourceKind,
    DateOnly? PeriodStart,
    DateOnly? PeriodEnd,
    IReadOnlyList<SpendingCurrencyTotalsDto> Totals,
    IReadOnlyList<SpendingTransactionDto> Transactions,
    IReadOnlyList<string> Warnings,
    DateTime CreatedAtUtc);

public sealed record UploadSpendingStatementCommand(
    string FileName,
    string ContentType,
    byte[] Content) : IRequest<SpendingStatementDetailDto>;

public sealed class UploadSpendingStatementCommandValidator : AbstractValidator<UploadSpendingStatementCommand>
{
    public const int MaxFileSize = 15 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".csv", ".xlsx", ".pdf",
    };

    public UploadSpendingStatementCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("Dosya adı zorunludur.")
            .Must(name => AllowedExtensions.Contains(Path.GetExtension(name)))
            .WithMessage("Desteklenen ekstre biçimleri CSV, XLSX ve metin tabanlı PDF'dir.");
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Ekstre dosyası zorunludur.")
            .Must(content => content.Length <= MaxFileSize).WithMessage("Ekstre dosyası en fazla 15 MB olabilir.");
        RuleFor(x => x)
            .Must(HasAllowedContentType)
            .WithMessage("Dosya içerik türü uzantıyla eşleşmiyor.");
        RuleFor(x => x)
            .Must(HasValidSignature)
            .WithMessage("Dosyanın uzantısı ile içeriği eşleşmiyor veya dosya geçerli değil.");
    }

    private static bool HasAllowedContentType(UploadSpendingStatementCommand command)
    {
        var contentType = command.ContentType.Split(';', 2)[0].Trim();
        if (contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase)) return true;
        return Path.GetExtension(command.FileName).ToLowerInvariant() switch
        {
            ".csv" => contentType.Equals("text/csv", StringComparison.OrdinalIgnoreCase)
                || contentType.Equals("application/csv", StringComparison.OrdinalIgnoreCase)
                || contentType.Equals("application/vnd.ms-excel", StringComparison.OrdinalIgnoreCase),
            ".xlsx" => contentType.Equals("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", StringComparison.OrdinalIgnoreCase),
            ".pdf" => contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase),
            _ => false,
        };
    }

    private static bool HasValidSignature(UploadSpendingStatementCommand command)
    {
        var extension = Path.GetExtension(command.FileName).ToLowerInvariant();
        var content = command.Content;
        if (extension == ".pdf")
        {
            return content.Length >= 5 && content.AsSpan(0, 5).SequenceEqual("%PDF-"u8);
        }
        if (extension == ".xlsx")
        {
            return content.Length >= 4 && content[0] == (byte)'P' && content[1] == (byte)'K'
                && content[2] == 0x03 && content[3] == 0x04;
        }
        if (extension != ".csv" || content.AsSpan(0, Math.Min(content.Length, 4096)).Contains((byte)0))
        {
            return false;
        }

        try
        {
            _ = new System.Text.UTF8Encoding(false, true).GetString(content);
            return true;
        }
        catch (System.Text.DecoderFallbackException)
        {
            return false;
        }
    }
}

public sealed class UploadSpendingStatementCommandHandler
    : IRequestHandler<UploadSpendingStatementCommand, SpendingStatementDetailDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ISpendingStatementParser _parser;

    public UploadSpendingStatementCommandHandler(
        IAppDbContext db,
        ICurrentUser currentUser,
        ISpendingStatementParser parser)
    {
        _db = db;
        _currentUser = currentUser;
        _parser = parser;
    }

    public async Task<SpendingStatementDetailDto> Handle(
        UploadSpendingStatementCommand request,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId(_currentUser);
        var sourceHash = Convert.ToHexString(SHA256.HashData(request.Content)).ToLowerInvariant();
        if (await _db.SpendingStatements.AsNoTracking().AnyAsync(
                x => x.OwnerUserId == userId && x.SourceHash == sourceHash,
                cancellationToken))
        {
            throw new ConflictException("Bu ekstre daha önce yüklenmiş.");
        }

        ParsedSpendingStatement parsed;
        try
        {
            parsed = await _parser.ParseAsync(
                request.Content,
                request.FileName,
                request.ContentType,
                cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ValidationException([
                new ValidationFailure(nameof(request.Content), "Ekstre okunamadı. Dosya biçimini ve sütun başlıklarını kontrol edin."),
            ]);
        }

        var merchantKeys = parsed.Transactions.Select(x => MerchantPreferenceKey.From(x.MerchantName)).Distinct().ToList();
        var learnedCategories = await _db.SpendingMerchantPreferences.AsNoTracking()
            .Where(x => x.OwnerUserId == userId && merchantKeys.Contains(x.MerchantKey))
            .ToDictionaryAsync(x => x.MerchantKey, x => x.Category, cancellationToken);

        var statement = new SpendingStatement
        {
            OwnerUserId = userId,
            SourceHash = sourceHash,
            SourceFileName = SafeFileName(request.FileName),
            SourceKind = parsed.SourceKind,
            PeriodStart = parsed.Transactions.Min(x => x.OccurredOn),
            PeriodEnd = parsed.Transactions.Max(x => x.OccurredOn),
            Transactions = parsed.Transactions.Select(transaction => new SpendingTransaction
            {
                OccurredOn = transaction.OccurredOn,
                Description = transaction.Description,
                MerchantName = transaction.MerchantName,
                Amount = transaction.Amount,
                IsRefund = transaction.IsRefund,
                Currency = transaction.Currency,
                Category = learnedCategories.GetValueOrDefault(
                    MerchantPreferenceKey.From(transaction.MerchantName), transaction.Category),
                InstallmentCurrent = transaction.InstallmentCurrent,
                InstallmentTotal = transaction.InstallmentTotal,
            }).ToList(),
        };

        return await _db.ExecuteInTransactionAsync(async ct =>
        {
            if (await _db.SpendingStatements.AsNoTracking().AnyAsync(
                    x => x.OwnerUserId == userId && x.SourceHash == sourceHash,
                    ct))
            {
                throw new ConflictException("Bu ekstre daha önce yüklenmiş.");
            }
            _db.SpendingStatements.Add(statement);
            await _db.SaveChangesAsync(ct);
            return SpendingStatementMappings.ToDetail(statement, parsed.Warnings);
        }, cancellationToken);
    }

    private static string SafeFileName(string fileName)
    {
        var redacted = SensitiveFinancialDataRedactor.Redact(Path.GetFileName(fileName));
        return redacted.Length <= 260 ? redacted : redacted[..260];
    }

    internal static string RequireUserId(ICurrentUser currentUser) => currentUser.UserId
        ?? throw new UnauthorizedAccessException("User is not authenticated.");
}

public sealed record ListSpendingStatementsQuery : IRequest<IReadOnlyList<SpendingStatementSummaryDto>>;

public sealed class ListSpendingStatementsQueryHandler
    : IRequestHandler<ListSpendingStatementsQuery, IReadOnlyList<SpendingStatementSummaryDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListSpendingStatementsQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<SpendingStatementSummaryDto>> Handle(
        ListSpendingStatementsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = UploadSpendingStatementCommandHandler.RequireUserId(_currentUser);
        var statements = await _db.SpendingStatements.AsNoTracking()
            .Include(x => x.Transactions)
            .Where(x => x.OwnerUserId == userId)
            .OrderByDescending(x => x.PeriodEnd)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        return statements.Select(SpendingStatementMappings.ToSummary).ToList();
    }
}

public sealed record GetSpendingStatementQuery(Guid StatementId) : IRequest<SpendingStatementDetailDto>;

public sealed class GetSpendingStatementQueryHandler
    : IRequestHandler<GetSpendingStatementQuery, SpendingStatementDetailDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetSpendingStatementQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<SpendingStatementDetailDto> Handle(
        GetSpendingStatementQuery request,
        CancellationToken cancellationToken)
    {
        var userId = UploadSpendingStatementCommandHandler.RequireUserId(_currentUser);
        var statement = await _db.SpendingStatements.AsNoTracking()
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.Id == request.StatementId && x.OwnerUserId == userId, cancellationToken)
            ?? throw new NotFoundException("Ekstre bulunamadı.");
        return SpendingStatementMappings.ToDetail(statement, []);
    }
}

public sealed record DeleteSpendingStatementCommand(Guid StatementId) : IRequest;

public sealed class DeleteSpendingStatementCommandHandler : IRequestHandler<DeleteSpendingStatementCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public DeleteSpendingStatementCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteSpendingStatementCommand request, CancellationToken cancellationToken)
    {
        var userId = UploadSpendingStatementCommandHandler.RequireUserId(_currentUser);
        var statement = await _db.SpendingStatements
            .FirstOrDefaultAsync(x => x.Id == request.StatementId && x.OwnerUserId == userId, cancellationToken)
            ?? throw new NotFoundException("Ekstre bulunamadı.");
        _db.SpendingStatements.Remove(statement);
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public sealed record ListSpendingCategoriesQuery : IRequest<IReadOnlyList<string>>;

public sealed class ListSpendingCategoriesQueryHandler
    : IRequestHandler<ListSpendingCategoriesQuery, IReadOnlyList<string>>
{
    public Task<IReadOnlyList<string>> Handle(
        ListSpendingCategoriesQuery request,
        CancellationToken cancellationToken) => Task.FromResult(SpendingCategoryCatalog.All);
}

public sealed record UpdateSpendingTransactionCategoryCommand(
    Guid StatementId,
    Guid TransactionId,
    string Category) : IRequest<SpendingTransactionDto>;

public sealed class UpdateSpendingTransactionCategoryCommandValidator
    : AbstractValidator<UpdateSpendingTransactionCategoryCommand>
{
    public UpdateSpendingTransactionCategoryCommandValidator()
    {
        RuleFor(x => x.Category)
            .Must(category => SpendingCategoryCatalog.All.Contains(category, StringComparer.Ordinal))
            .WithMessage("Geçerli bir harcama kategorisi seçin.");
    }
}

public sealed class UpdateSpendingTransactionCategoryCommandHandler
    : IRequestHandler<UpdateSpendingTransactionCategoryCommand, SpendingTransactionDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public UpdateSpendingTransactionCategoryCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<SpendingTransactionDto> Handle(
        UpdateSpendingTransactionCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var userId = UploadSpendingStatementCommandHandler.RequireUserId(_currentUser);
        var transaction = await _db.SpendingTransactions.FirstOrDefaultAsync(
            item => item.Id == request.TransactionId
                && item.StatementId == request.StatementId
                && _db.SpendingStatements.Any(statement =>
                    statement.Id == item.StatementId && statement.OwnerUserId == userId),
            cancellationToken) ?? throw new NotFoundException("İşlem bulunamadı.");

        transaction.Category = request.Category;
        var merchantKey = MerchantPreferenceKey.From(transaction.MerchantName);
        var preference = await _db.SpendingMerchantPreferences.FirstOrDefaultAsync(
            x => x.OwnerUserId == userId && x.MerchantKey == merchantKey, cancellationToken);
        if (preference is null)
        {
            _db.SpendingMerchantPreferences.Add(new SpendingMerchantPreference
            {
                OwnerUserId = userId,
                MerchantKey = merchantKey,
                Category = request.Category,
            });
        }
        else preference.Category = request.Category;
        await _db.SaveChangesAsync(cancellationToken);
        return SpendingStatementMappings.ToTransaction(transaction);
    }
}

internal static class MerchantPreferenceKey
{
    public static string From(string merchantName) => string.Join(' ', merchantName
        .Trim().ToUpperInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}

internal static class SpendingStatementMappings
{
    public static SpendingStatementSummaryDto ToSummary(SpendingStatement statement) => new(
        statement.Id,
        statement.SourceFileName,
        statement.SourceKind,
        statement.PeriodStart,
        statement.PeriodEnd,
        statement.Transactions.Count,
        Totals(statement.Transactions),
        statement.CreatedAtUtc);

    public static SpendingStatementDetailDto ToDetail(
        SpendingStatement statement,
        IReadOnlyList<string> warnings) => new(
        statement.Id,
        statement.SourceFileName,
        statement.SourceKind,
        statement.PeriodStart,
        statement.PeriodEnd,
        Totals(statement.Transactions),
        statement.Transactions.OrderByDescending(x => x.OccurredOn).Select(ToTransaction).ToList(),
        warnings,
        statement.CreatedAtUtc);

    public static SpendingTransactionDto ToTransaction(SpendingTransaction transaction) => new(
        transaction.Id,
        transaction.OccurredOn,
        transaction.Description,
        transaction.MerchantName,
        transaction.Amount,
        transaction.IsRefund,
        transaction.Currency,
        transaction.Category,
        transaction.InstallmentCurrent,
        transaction.InstallmentTotal);

    private static IReadOnlyList<SpendingCurrencyTotalsDto> Totals(IEnumerable<SpendingTransaction> transactions) =>
        transactions.GroupBy(x => x.Currency).OrderBy(x => x.Key).Select(group =>
        {
            var spending = group.Where(x => !x.IsRefund).Sum(x => x.Amount);
            var refunds = group.Where(x => x.IsRefund).Sum(x => x.Amount);
            return new SpendingCurrencyTotalsDto(group.Key, spending, refunds, spending - refunds);
        }).ToList();
}
