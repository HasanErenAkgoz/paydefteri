using PayDefteri.Domain.Entities;

namespace PayDefteri.Domain.Templates;

/// <summary>Backward-compatible wrapper around <see cref="PlanTemplateCatalog"/>.</summary>
public static class FuzulSeedTemplate
{
    public static decimal ExpectedGrandTotal => 1_070_000m;

    public static (Plan Plan, List<Partner> Partners, List<Installment> Installments) Create(string ownerUserId)
    {
        var def = PlanTemplateCatalog.Get("fuzul");
        var plan = new Plan
        {
            OwnerUserId = ownerUserId,
            Title = def.Title,
            Description = def.Description
        };

        var (partners, installments, deliveryId) = PlanTemplateCatalog.Materialize(def, plan.Id);
        plan.DeliveryInstallmentId = deliveryId;
        return (plan, partners, installments);
    }
}
