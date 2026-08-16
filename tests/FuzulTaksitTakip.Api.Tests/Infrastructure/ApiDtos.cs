using System.Text.Json;

namespace FuzulTaksitTakip.Api.Tests.Infrastructure;

public sealed record PlanDto(
    Guid Id,
    string Title,
    string Description,
    Guid? DeliveryInstallmentId,
    DateTime CreatedAtUtc,
    bool IsArchived = false);

public sealed record PartnerDto(
    Guid Id,
    Guid PlanId,
    string Name,
    string Color,
    decimal DefaultPct,
    int SortOrder,
    string? LinkedUserId = null,
    string? Iban = null,
    string? InviteEmail = null);

public sealed record CustomShareDto(Guid PartnerId, decimal Amount);

public sealed record InstallmentDto(
    Guid Id,
    Guid PlanId,
    string Name,
    DateOnly DueDate,
    decimal TotalAmount,
    string ShareType,
    int SortOrder,
    IReadOnlyList<CustomShareDto> CustomShares,
    IReadOnlyList<PaymentDto> Payments);

public sealed record PaymentDto(
    Guid PartnerId,
    bool IsPaid,
    DateOnly? PaidAt,
    Guid? PaidByPartnerId,
    string Note,
    bool HasReceipt,
    string ReviewStatus);

public sealed record ExpenseBoardDto(
    IReadOnlyList<ExpenseBalanceDto> Balances,
    IReadOnlyList<ExpenseDto> Expenses,
    IReadOnlyList<SettlementTransferDto> Transfers);

public sealed record ExpenseBalanceDto(Guid PartnerId, decimal Balance);

public sealed record ExpensePaymentDto(Guid PartnerId, decimal Amount);

public sealed record ExpenseDto(
    Guid Id,
    string Name,
    decimal TotalAmount,
    bool CanManage = false,
    DateOnly OccurredOn = default,
    JsonElement Status = default,
    IReadOnlyList<CustomShareDto>? CustomShares = null,
    IReadOnlyList<ExpensePaymentDto>? Payments = null);

public sealed record SettlementTransferDto(Guid Id, decimal Amount);
