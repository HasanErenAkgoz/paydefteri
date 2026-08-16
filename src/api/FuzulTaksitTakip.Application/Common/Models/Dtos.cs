using FuzulTaksitTakip.Domain.Enums;

namespace FuzulTaksitTakip.Application.Common.Models;

public sealed record PlanDto(
    Guid Id,
    PlanType PlanType,
    string Title,
    string Description,
    Guid? DeliveryInstallmentId,
    DateTime CreatedAtUtc,
    bool RequireReceipt,
    IbanMode IbanMode,
    string? SettlementIban,
    bool RemindersEnabled,
    IReadOnlyList<int> ReminderDaysBefore,
    IReadOnlyList<int> ReminderDaysAfter,
    bool IsArchived = false);

public sealed record ExpenseCategoryDto(Guid Id, Guid PlanId, string Name, string Color, int SortOrder);

public sealed record ExpensePaymentDto(Guid PartnerId, decimal Amount);

public sealed record ExpenseDto(
    Guid Id,
    Guid PlanId,
    Guid? CategoryId,
    string? CategoryName,
    Guid? RecurrenceId,
    string Name,
    DateOnly OccurredOn,
    decimal TotalAmount,
    ShareType ShareType,
    ExpenseStatus Status,
    Guid? PaidByPartnerId,
    string Note,
    IReadOnlyList<CustomShareDto> CustomShares,
    IReadOnlyList<ExpenseShareLineDto> ShareLines,
    IReadOnlyList<ExpensePaymentDto> Payments,
    bool CanManage = false);

public sealed record ExpenseShareLineDto(Guid PartnerId, string PartnerName, decimal ShareAmount);

public sealed record ExpenseRecurrenceDto(
    Guid Id,
    Guid PlanId,
    Guid? CategoryId,
    string Name,
    decimal TotalAmount,
    ShareType ShareType,
    Guid? DefaultPaidByPartnerId,
    RecurrenceFrequency Frequency,
    int AnchorDay,
    DateOnly StartDate,
    DateOnly? EndDate,
    DateOnly NextOccurrence,
    bool IsActive,
    string Note,
    IReadOnlyList<CustomShareDto> CustomShares);

public sealed record SettlementTransferDto(
    Guid Id,
    Guid PlanId,
    Guid FromPartnerId,
    string FromPartnerName,
    Guid ToPartnerId,
    string ToPartnerName,
    decimal Amount,
    DateOnly TransferredOn,
    string Note);

public sealed record ExpenseBalanceDto(Guid PartnerId, string PartnerName, string Color, decimal Balance);

public sealed record ExpenseBoardDto(
    PlanDto Plan,
    IReadOnlyList<ExpenseBalanceDto> Balances,
    IReadOnlyList<ExpenseDto> Expenses,
    IReadOnlyList<ExpenseCategoryDto> Categories,
    IReadOnlyList<ExpenseRecurrenceDto> Recurrences,
    IReadOnlyList<SettlementTransferDto> Transfers,
    bool IsOwner = false);

public sealed record PagedExpenseDto(
    IReadOnlyList<ExpenseDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record PartnerDto(
    Guid Id,
    Guid PlanId,
    string Name,
    string Color,
    decimal DefaultPct,
    int SortOrder,
    string? LinkedUserId,
    string? Iban,
    string? InviteEmail = null);

public sealed record CustomShareDto(Guid PartnerId, decimal Amount);

public sealed record PaymentDto(
    Guid PartnerId,
    bool IsPaid,
    DateOnly? PaidAt,
    Guid? PaidByPartnerId,
    string Note,
    bool HasReceipt,
    PaymentReviewStatus ReviewStatus = PaymentReviewStatus.None);

public sealed record InstallmentDto(
    Guid Id,
    Guid PlanId,
    string Name,
    DateOnly DueDate,
    decimal TotalAmount,
    ShareType ShareType,
    int SortOrder,
    IReadOnlyList<CustomShareDto> CustomShares,
    IReadOnlyList<PaymentDto> Payments);

public sealed record PartnerPaymentStatusDto(
    Guid PartnerId,
    string PartnerName,
    decimal ShareAmount,
    bool IsPaid,
    DateOnly? PaidAt,
    Guid? PaidByPartnerId,
    string Note,
    bool HasReceipt,
    PaymentReviewStatus ReviewStatus = PaymentReviewStatus.None);

public sealed record DashboardInstallmentDto(
    Guid Id,
    string Name,
    DateOnly DueDate,
    decimal TotalAmount,
    ShareType ShareType,
    InstallmentStatus Status,
    int SortOrder,
    IReadOnlyList<PartnerPaymentStatusDto> PartnerPayments);

public sealed record PartnerSummaryDto(
    Guid PartnerId,
    string Name,
    string Color,
    decimal TotalShare,
    decimal PaidAmount,
    decimal RemainingAmount,
    string? Iban);

public sealed record SettlementBalanceDto(
    Guid PartnerId,
    string PartnerName,
    decimal Balance);

public sealed record DashboardMetricsDto(
    decimal GrandTotal,
    decimal GrandPaid,
    decimal GrandRemaining,
    decimal PaidPercent);

public sealed record MyShareMetricsDto(
    decimal RemainingAmount,
    decimal PaidAmount,
    decimal TotalShare,
    int UnpaidInstallmentCount,
    DateOnly? NextDueDate,
    string? NextInstallmentName);

public sealed record DashboardDto(
    Guid PlanId,
    string Title,
    string Description,
    Guid? DeliveryInstallmentId,
    int? DaysUntilDelivery,
    Guid? MyPartnerId,
    bool IsOwner,
    bool RequireReceipt,
    IbanMode IbanMode,
    string? SettlementIban,
    string? PaymentTargetIban,
    DashboardMetricsDto Metrics,
    IReadOnlyList<PartnerSummaryDto> Partners,
    IReadOnlyList<SettlementBalanceDto> Settlements,
    IReadOnlyList<DashboardInstallmentDto> Installments,
    MyShareMetricsDto? MyMetrics = null,
    int PendingApprovalCount = 0);

public sealed record PlanMemberDto(
    Guid Id,
    string UserId,
    string? Email,
    string? DisplayName,
    string Role,
    Guid? PartnerId,
    string? PartnerName);

public sealed record PlanInviteDto(
    Guid Id,
    string Email,
    Guid PartnerId,
    string PartnerName,
    string Status,
    string Token,
    DateTime ExpiresAtUtc,
    DateTime CreatedAtUtc,
    bool EmailSent = false);

public sealed record InvitePreviewDto(
    string Token,
    string Email,
    string PartnerName,
    string PlanTitle,
    string Status,
    DateTime ExpiresAtUtc,
    bool IsAcceptable,
    bool AccountExists = false);

public sealed record PlanExportDto(
    string Title,
    string Description,
    Guid? DeliveryInstallmentId,
    IReadOnlyList<PartnerExportDto> Partners,
    IReadOnlyList<InstallmentExportDto> Installments);

public sealed record PartnerExportDto(
    Guid Id,
    string Name,
    string Color,
    decimal DefaultPct,
    int SortOrder);

public sealed record InstallmentExportDto(
    Guid Id,
    string Name,
    DateOnly DueDate,
    decimal TotalAmount,
    string ShareType,
    int SortOrder,
    IReadOnlyList<CustomShareDto> CustomShares,
    IReadOnlyList<PaymentDto> Payments);

public sealed record ReminderHistoryItemDto(
    Guid Id,
    Guid InstallmentId,
    string InstallmentName,
    Guid? PartnerId,
    string? PartnerName,
    string Kind,
    int OffsetDays,
    DateOnly SentOn,
    DateTime CreatedAtUtc);

public sealed record ReportPartnerBarDto(
    Guid PartnerId,
    string Name,
    string Color,
    decimal PaidAmount,
    decimal RemainingAmount,
    decimal TotalShare);

public sealed record ReportMonthDto(
    string YearMonth,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    int InstallmentCount);

public sealed record ReportSummaryDto(
    IReadOnlyList<ReportPartnerBarDto> Partners,
    IReadOnlyList<ReportMonthDto> Months,
    DashboardMetricsDto Metrics);

public sealed record PlanActivityItemDto(
    Guid Id,
    string Type,
    string Message,
    string ActorDisplayName,
    DateTime CreatedAtUtc);
