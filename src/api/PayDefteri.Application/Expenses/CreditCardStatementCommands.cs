using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PayDefteri.Application.Common.Interfaces;
using PayDefteri.Application.Common.Models;
using PayDefteri.Domain.Entities;
using PayDefteri.Domain.Enums;

namespace PayDefteri.Application.Expenses;

public sealed record StatementSummaryDto(string? BankName, string? CardLastDigits, string? StatementPeriod, DateOnly? DueDate, decimal? TotalDebt, decimal? MinimumPayment);
public sealed record StatementTransactionItemDto(string TempId, DateOnly OccurredOn, string Description, decimal Amount, bool IsIncomeOrRefund, int? InstallmentCurrent, int? InstallmentTotal, Guid? SuggestedCategoryId, string? SuggestedCategoryName, string? MerchantName, decimal Confidence, string? Note, bool IsPossibleDuplicate, Guid? ExistingExpenseId, string? ExistingExpenseName);
public sealed record CreditCardStatementAnalysisResultDto(StatementSummaryDto Summary, IReadOnlyList<StatementTransactionItemDto> Transactions, IReadOnlyList<string> Warnings);
public sealed record StatementExpenseImportItemDto(string Name, DateOnly OccurredOn, decimal TotalAmount, ShareType ShareType, ExpenseStatus Status, Guid? PaidByPartnerId, Guid? CategoryId, string? Note, IReadOnlyList<CustomShareDto>? CustomShares, IReadOnlyList<ExpensePaymentDto>? Payments, int InstallmentCount = 1, bool CreateRemainingInstallments = false);
public sealed record ImportStatementExpensesResultDto(int ImportedCount, decimal TotalAmount, IReadOnlyList<Guid> ExpenseIds);

public sealed record AnalyzeCreditCardStatementCommand(Guid PlanId, string ContentType, byte[] Content, string FileName, Guid? DefaultPaidByPartnerId) : IRequest<CreditCardStatementAnalysisResultDto>;

public sealed class AnalyzeCreditCardStatementCommandValidator : AbstractValidator<AnalyzeCreditCardStatementCommand>
{
    private const int MaxFileSize = 15 * 1024 * 1024;
    public AnalyzeCreditCardStatementCommandValidator()
    {
        RuleFor(x => x.Content).NotEmpty().Must(content => content.Length <= MaxFileSize).WithMessage("Ekstre dosyası en fazla 15 MB olabilir.");
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(260);
    }
}

public sealed class AnalyzeCreditCardStatementCommandHandler : IRequestHandler<AnalyzeCreditCardStatementCommand, CreditCardStatementAnalysisResultDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _authorization;
    private readonly ICurrentUser _currentUser;
    private readonly ICreditCardStatementAnalyzer _analyzer;

    public AnalyzeCreditCardStatementCommandHandler(IAppDbContext db, IPlanAuthorization authorization, ICurrentUser currentUser, ICreditCardStatementAnalyzer analyzer)
        => (_db, _authorization, _currentUser, _analyzer) = (db, authorization, currentUser, analyzer);

    public async Task<CreditCardStatementAnalysisResultDto> Handle(AnalyzeCreditCardStatementCommand request, CancellationToken ct)
    {
        await ExpensePlanGuards.EnsureExpensePlanAsync(_db, _authorization, request.PlanId, ct);
        var categories = await _db.ExpenseCategories.AsNoTracking().Where(x => x.PlanId == request.PlanId && !x.IsDeleted).Select(x => new { x.Id, x.Name }).ToListAsync(ct);
        var existing = await _db.Expenses.AsNoTracking().Where(x => x.PlanId == request.PlanId && !x.IsDeleted).OrderByDescending(x => x.OccurredOn).Take(500).Select(x => new { x.Id, x.Name, x.OccurredOn, x.TotalAmount }).ToListAsync(ct);
        var user = _currentUser.UserId ?? "anonymous";
        var result = await _analyzer.AnalyzeAsync(new CreditCardStatementAnalysisInput(request.ContentType, request.Content, request.FileName, categories.Select(x => x.Name).ToList(), Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(user))).ToLowerInvariant()), ct);
        var rows = result.Transactions.Select(transaction =>
        {
            var category = categories.FirstOrDefault(x => !string.IsNullOrWhiteSpace(transaction.CategoryName) && string.Equals(x.Name, transaction.CategoryName, StringComparison.OrdinalIgnoreCase));
            var duplicate = existing.FirstOrDefault(x => x.OccurredOn == transaction.OccurredOn && Math.Abs(x.TotalAmount - transaction.Amount) < .01m && string.Equals(x.Name, transaction.MerchantName ?? transaction.Description, StringComparison.OrdinalIgnoreCase));
            return new StatementTransactionItemDto(Guid.NewGuid().ToString("N"), transaction.OccurredOn, transaction.Description, transaction.Amount, transaction.IsIncomeOrRefund, transaction.InstallmentCurrent, transaction.InstallmentTotal, category?.Id, category?.Name, transaction.MerchantName, Math.Clamp(transaction.Confidence, 0, 1), transaction.Note, duplicate is not null, duplicate?.Id, duplicate?.Name);
        }).ToList();
        return new CreditCardStatementAnalysisResultDto(new StatementSummaryDto(result.BankName, result.CardLastDigits, result.StatementPeriod, result.DueDate, result.TotalDebt, result.MinimumPayment), rows, result.Warnings);
    }
}

public sealed record ImportCreditCardExpensesCommand(Guid PlanId, IReadOnlyList<StatementExpenseImportItemDto> Items) : IRequest<ImportStatementExpensesResultDto>;

public sealed class ImportCreditCardExpensesCommandHandler : IRequestHandler<ImportCreditCardExpensesCommand, ImportStatementExpensesResultDto>
{
    private readonly ISender _sender;
    public ImportCreditCardExpensesCommandHandler(ISender sender) => _sender = sender;

    public async Task<ImportStatementExpensesResultDto> Handle(ImportCreditCardExpensesCommand request, CancellationToken ct)
    {
        if (request.Items.Count is < 1 or > 200) throw new ValidationException("Bir seferde 1 ile 200 arası işlem içe aktarılabilir.");
        var created = new List<Guid>();
        decimal total = 0;
        foreach (var item in request.Items)
        {
            if (string.IsNullOrWhiteSpace(item.Name) || item.TotalAmount <= 0) throw new ValidationException("İçe aktarılacak işlem bilgileri geçersiz.");
            var expense = await _sender.Send(new CreateExpenseCommand(request.PlanId, item.Name, item.OccurredOn, item.TotalAmount, item.ShareType, item.Status, item.PaidByPartnerId, item.CategoryId, item.Note, item.CustomShares, item.Payments, item.CreateRemainingInstallments ? Math.Clamp(item.InstallmentCount, 1, 120) : 1), ct);
            created.Add(expense.Id);
            total += item.TotalAmount;
        }
        return new ImportStatementExpensesResultDto(created.Count, decimal.Round(total, 2), created);
    }
}
