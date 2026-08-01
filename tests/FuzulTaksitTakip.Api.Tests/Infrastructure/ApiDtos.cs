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
    string? Iban = null);

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
