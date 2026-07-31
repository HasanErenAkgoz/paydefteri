using FuzulTaksitTakip.Domain.Enums;

namespace FuzulTaksitTakip.Application.Common.Models;

public sealed record PlanDto(
    Guid Id,
    string Title,
    string Description,
    Guid? DeliveryInstallmentId,
    DateTime CreatedAtUtc);

public sealed record PartnerDto(
    Guid Id,
    Guid PlanId,
    string Name,
    string Color,
    decimal DefaultPct,
    int SortOrder);

public sealed record CustomShareDto(Guid PartnerId, decimal Amount);

public sealed record PaymentDto(
    Guid PartnerId,
    bool IsPaid,
    DateOnly? PaidAt,
    Guid? PaidByPartnerId,
    string Note);

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
    string Note);

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
    decimal RemainingAmount);

public sealed record SettlementBalanceDto(
    Guid PartnerId,
    string PartnerName,
    decimal Balance);

public sealed record DashboardMetricsDto(
    decimal GrandTotal,
    decimal GrandPaid,
    decimal GrandRemaining,
    decimal PaidPercent);

public sealed record DashboardDto(
    Guid PlanId,
    string Title,
    string Description,
    Guid? DeliveryInstallmentId,
    int? DaysUntilDelivery,
    DashboardMetricsDto Metrics,
    IReadOnlyList<PartnerSummaryDto> Partners,
    IReadOnlyList<SettlementBalanceDto> Settlements,
    IReadOnlyList<DashboardInstallmentDto> Installments);

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
