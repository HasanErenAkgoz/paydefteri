namespace PayDefteri.Domain.Enums;

public enum PlanActivityType
{
    PaymentUpserted = 0,
    PaymentApproved = 1,
    PaymentRejected = 2,
    SettleUp = 3,
    InstallmentCreated = 4,
    InstallmentUpdated = 5,
    InstallmentDeleted = 6,
    PartnerCreated = 7,
    PartnerUpdated = 8,
    PartnerDeleted = 9,
    PlanSeeded = 10,
    PlanImported = 11,
    PlanArchived = 12,
    PlanRestored = 13,
    PlanCopied = 14,
    PlanUpdated = 15
}
