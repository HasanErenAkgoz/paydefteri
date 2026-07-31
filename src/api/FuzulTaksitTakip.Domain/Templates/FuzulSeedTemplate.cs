using FuzulTaksitTakip.Domain.Entities;
using FuzulTaksitTakip.Domain.Enums;

namespace FuzulTaksitTakip.Domain.Templates;

public static class FuzulSeedTemplate
{
    public static decimal ExpectedGrandTotal => 1_070_000m;

    public static (Plan Plan, List<Partner> Partners, List<Installment> Installments) Create(string ownerUserId)
    {
        var plan = new Plan
        {
            OwnerUserId = ownerUserId,
            Title = "Fuzul Ev Konut Projesi",
            Description = "Eren & Yusuf Ortak Ödeme Planı (1,070,000 ₺ Toplam Maliyet)"
        };

        var eren = new Partner
        {
            PlanId = plan.Id,
            Name = "Eren",
            Color = "#38bdf8",
            DefaultPct = 50m,
            SortOrder = 0
        };
        var yusuf = new Partner
        {
            PlanId = plan.Id,
            Name = "Yusuf",
            Color = "#fb923c",
            DefaultPct = 50m,
            SortOrder = 1
        };

        var rows = new (string Name, string Date, decimal Amount, bool BothPaid)[]
        {
            ("Peşinat + 1. Tasarruf Taksiti + Org. Peşinatı", "2026-05-05", 310000m, true),
            ("2. Tasarruf Taksiti + Org. Ücreti", "2026-06-05", 33750m, true),
            ("3. Tasarruf Taksiti + Org. Ücreti", "2026-07-05", 33750m, true),
            ("4. Tasarruf Taksiti + Org. Ücreti", "2026-08-05", 33750m, false),
            ("5. Tasarruf Taksiti + Org. Ücreti", "2026-09-05", 33750m, false),
            ("6. Tahsisat Taksiti", "2026-10-05", 25000m, false),
            ("7. Finansman Taksiti", "2026-11-05", 25000m, false),
            ("8. Finansman Taksiti", "2026-12-05", 25000m, false),
            ("9. Finansman Taksiti", "2027-01-05", 25000m, false),
            ("10. Finansman Taksiti", "2027-02-05", 25000m, false),
            ("11. Finansman Taksiti", "2027-03-05", 25000m, false),
            ("12. Finansman Taksiti", "2027-04-05", 25000m, false),
            ("13. Finansman Taksiti", "2027-05-05", 25000m, false),
            ("14. Finansman Taksiti", "2027-06-05", 25000m, false),
            ("15. Finansman Taksiti", "2027-07-05", 25000m, false),
            ("16. Finansman Taksiti", "2027-08-05", 68250m, false),
            ("17. Finansman Taksiti", "2027-09-05", 68250m, false),
            ("18. Finansman Taksiti", "2027-10-05", 68250m, false),
            ("19. Finansman Taksiti", "2027-11-05", 68250m, false),
            ("20. Son Finansman Taksiti", "2027-12-05", 102000m, false),
        };

        var installments = new List<Installment>();
        for (var i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            var inst = new Installment
            {
                PlanId = plan.Id,
                Name = row.Name,
                DueDate = DateOnly.Parse(row.Date),
                TotalAmount = row.Amount,
                ShareType = ShareType.Default,
                SortOrder = i
            };

            if (row.BothPaid)
            {
                inst.Payments.Add(new Payment
                {
                    InstallmentId = inst.Id,
                    PartnerId = eren.Id,
                    IsPaid = true,
                    PaidByPartnerId = eren.Id,
                    PaidAt = row.Date is var d ? DateOnly.Parse(d) : null
                });
                inst.Payments.Add(new Payment
                {
                    InstallmentId = inst.Id,
                    PartnerId = yusuf.Id,
                    IsPaid = true,
                    PaidByPartnerId = yusuf.Id,
                    PaidAt = DateOnly.Parse(row.Date)
                });
            }

            installments.Add(inst);
        }

        plan.DeliveryInstallmentId = installments[5].Id; // 6. Tahsisat
        return (plan, new List<Partner> { eren, yusuf }, installments);
    }
}
