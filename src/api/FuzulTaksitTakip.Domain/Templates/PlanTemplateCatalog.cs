using FuzulTaksitTakip.Domain.Entities;
using FuzulTaksitTakip.Domain.Enums;

namespace FuzulTaksitTakip.Domain.Templates;

public sealed record TemplatePartnerDef(string Name, string Color, decimal DefaultPct);

public sealed record TemplateInstallmentDef(
    string Name,
    DateOnly DueDate,
    decimal TotalAmount,
    bool MarkAllPartnersPaid = false);

public sealed record PlanTemplateDefinition(
    string Key,
    string Title,
    string Description,
    int DeliveryIndex,
    IReadOnlyList<TemplatePartnerDef> Partners,
    IReadOnlyList<TemplateInstallmentDef> Installments);

public sealed record PlanTemplatePreview(
    string Key,
    string Title,
    string Description,
    decimal GrandTotal,
    int InstallmentCount,
    string? DeliveryName,
    IReadOnlyList<TemplateInstallmentDef> Installments,
    int PartnerCount);

public static class PlanTemplateCatalog
{
    public static readonly IReadOnlyList<string> Keys =
        new[] { "fuzul", "eminevim", "birevim", "katilimevim", "sinpas", "empty" };

    public static bool TryGet(string key, out PlanTemplateDefinition? definition)
    {
        definition = key.Trim().ToLowerInvariant() switch
        {
            "fuzul" => Fuzul(),
            "eminevim" => Eminevim(),
            "birevim" => Birevim(),
            "katilimevim" => Katilimevim(),
            "sinpas" => Sinpas(),
            "empty" => Empty(),
            _ => null
        };
        return definition is not null;
    }

    public static PlanTemplateDefinition Get(string key)
    {
        if (!TryGet(key, out var def) || def is null)
        {
            throw new ArgumentException($"Unknown template key: {key}", nameof(key));
        }

        return def;
    }

    public static PlanTemplatePreview ToPreview(PlanTemplateDefinition def)
    {
        var total = def.Installments.Sum(i => i.TotalAmount);
        string? delivery = null;
        if (def.DeliveryIndex >= 0 && def.DeliveryIndex < def.Installments.Count)
        {
            delivery = def.Installments[def.DeliveryIndex].Name;
        }

        return new PlanTemplatePreview(
            def.Key,
            def.Title,
            def.Description,
            total,
            def.Installments.Count,
            delivery,
            def.Installments,
            def.Partners.Count);
    }

    /// <summary>
    /// Builds partners + installments bound to <paramref name="planId"/>.
    /// Returns delivery installment id when applicable.
    /// </summary>
    public static (List<Partner> Partners, List<Installment> Installments, Guid? DeliveryInstallmentId)
        Materialize(PlanTemplateDefinition def, Guid planId)
    {
        var partners = new List<Partner>();
        for (var i = 0; i < def.Partners.Count; i++)
        {
            var p = def.Partners[i];
            partners.Add(new Partner
            {
                PlanId = planId,
                Name = p.Name,
                Color = p.Color,
                DefaultPct = p.DefaultPct,
                SortOrder = i
            });
        }

        var installments = new List<Installment>();
        Guid? deliveryId = null;

        for (var i = 0; i < def.Installments.Count; i++)
        {
            var row = def.Installments[i];
            var inst = new Installment
            {
                PlanId = planId,
                Name = row.Name,
                DueDate = row.DueDate,
                TotalAmount = row.TotalAmount,
                ShareType = ShareType.Default,
                SortOrder = i
            };

            if (row.MarkAllPartnersPaid)
            {
                foreach (var partner in partners)
                {
                    inst.Payments.Add(new Payment
                    {
                        InstallmentId = inst.Id,
                        PartnerId = partner.Id,
                        IsPaid = true,
                        PaidByPartnerId = partner.Id,
                        PaidAt = row.DueDate
                    });
                }
            }

            if (i == def.DeliveryIndex)
            {
                deliveryId = inst.Id;
            }

            installments.Add(inst);
        }

        return (partners, installments, deliveryId);
    }

    private static PlanTemplateDefinition Empty() => new(
        "empty",
        "Yeni Özel Plan",
        "Boş plan — ortak ve taksitleri kendiniz ekleyin.",
        -1,
        Array.Empty<TemplatePartnerDef>(),
        Array.Empty<TemplateInstallmentDef>());

    private static PlanTemplateDefinition Fuzul() => new(
        "fuzul",
        "Fuzul Ev Konut Projesi",
        "Eren & Yusuf Ortak Ödeme Planı (1,070,000 ₺ Toplam Maliyet)",
        5,
        new[]
        {
            new TemplatePartnerDef("Eren", "#38bdf8", 50m),
            new TemplatePartnerDef("Yusuf", "#fb923c", 50m)
        },
        new[]
        {
            Row("Peşinat + 1. Tasarruf Taksiti + Org. Peşinatı", 2026, 5, 5, 310000m, true),
            Row("2. Tasarruf Taksiti + Org. Ücreti 1. Taksiti", 2026, 6, 5, 33750m, true),
            Row("3. Tasarruf Taksiti + Org. Ücreti 2. Taksiti", 2026, 7, 5, 33750m, true),
            Row("4. Tasarruf Taksiti + Org. Ücreti 3. Taksiti", 2026, 8, 5, 33750m),
            Row("5. Tasarruf Taksiti + Org. Ücreti 4. Taksiti", 2026, 9, 5, 33750m),
            Row("6. Tahsisat Taksiti (Ev Teslimat Ayı)", 2026, 10, 5, 25000m),
            Row("7. Finansman Taksiti", 2026, 11, 5, 25000m),
            Row("8. Finansman Taksiti", 2026, 12, 5, 25000m),
            Row("9. Finansman Taksiti", 2027, 1, 5, 25000m),
            Row("10. Finansman Taksiti", 2027, 2, 5, 25000m),
            Row("11. Finansman Taksiti", 2027, 3, 5, 25000m),
            Row("12. Finansman Taksiti", 2027, 4, 5, 25000m),
            Row("13. Finansman Taksiti", 2027, 5, 5, 25000m),
            Row("14. Finansman Taksiti", 2027, 6, 5, 25000m),
            Row("15. Finansman Taksiti", 2027, 7, 5, 25000m),
            Row("16. Finansman Taksiti", 2027, 8, 5, 68250m),
            Row("17. Finansman Taksiti", 2027, 9, 5, 68250m),
            Row("18. Finansman Taksiti", 2027, 10, 5, 68250m),
            Row("19. Finansman Taksiti", 2027, 11, 5, 68250m),
            Row("20. Finansman Taksiti", 2027, 12, 5, 102000m)
        });

    private static PlanTemplateDefinition Eminevim() => new(
        "eminevim",
        "Eminevim Çekilişli Konut Planı",
        "1,500,000 ₺ Konut + 105,000 ₺ Organizasyon Ücreti (60 Ay Sabit Model)",
        5,
        TwoPartners("Eren", "Yusuf"),
        new[]
        {
            Row("Peşinat (300.000 ₺) + Org. Ücreti Peşinat (52.500 ₺)", 2026, 5, 5, 352500m, true),
            Row("Org. Ücreti 2. Taksit (52.500 ₺) + 1. Tasarruf (25.000 ₺)", 2026, 6, 5, 77500m, true),
            Row("2. Tasarruf Taksiti", 2026, 7, 5, 25000m),
            Row("3. Tasarruf Taksiti", 2026, 8, 5, 25000m),
            Row("4. Tasarruf Taksiti", 2026, 9, 5, 25000m),
            Row("5. Tasarruf Taksiti (Tahsisat Ayı)", 2026, 10, 5, 25000m),
            Row("6. Tasarruf Taksiti", 2026, 11, 5, 25000m),
            Row("7. Tasarruf Taksiti", 2026, 12, 5, 25000m),
            Row("8. Tasarruf Taksiti", 2027, 1, 5, 25000m),
            Row("9. Tasarruf Taksiti", 2027, 2, 5, 25000m),
            Row("10. Tasarruf Taksiti", 2027, 3, 5, 25000m),
            Row("11. Tasarruf Taksiti", 2027, 4, 5, 25000m)
        });

    private static PlanTemplateDefinition Birevim() => new(
        "birevim",
        "Birevim Serbest Planlı Araç Finansmanı",
        "500,000 ₺ Araç Finansmanı + 40,000 ₺ Çalışma Bedeli (Peşinatsız 24 Ay Model)",
        5,
        TwoPartners("Ortak 1", "Ortak 2"),
        new[]
        {
            Row("Çalışma Bedeli 1. Taksit (10.000 ₺) + 1. Tasarruf (20.833 ₺)", 2026, 5, 1, 30833m),
            Row("Çalışma Bedeli 2. Taksit (10.000 ₺) + 2. Tasarruf (20.833 ₺)", 2026, 6, 1, 30833m),
            Row("Çalışma Bedeli 3. Taksit (10.000 ₺) + 3. Tasarruf (20.833 ₺)", 2026, 7, 1, 30833m),
            Row("Çalışma Bedeli 4. Taksit (10.000 ₺) + 4. Tasarruf (20.833 ₺)", 2026, 8, 1, 30833m),
            Row("5. Tasarruf Taksiti", 2026, 9, 1, 20833m),
            Row("6. Tasarruf Taksiti (Teslimat Ayı)", 2026, 10, 1, 20833m),
            Row("7. Tasarruf Taksiti", 2026, 11, 1, 20833m),
            Row("8. Tasarruf Taksiti", 2026, 12, 1, 20833m),
            Row("9. Tasarruf Taksiti", 2027, 1, 1, 20833m),
            Row("10. Tasarruf Taksiti", 2027, 2, 1, 20833m),
            Row("11. Tasarruf Taksiti", 2027, 3, 1, 20833m),
            Row("12. Tasarruf Taksiti", 2027, 4, 1, 20833m)
        });

    private static PlanTemplateDefinition Katilimevim() => new(
        "katilimevim",
        "Katılımevim Tasarruf Dönemli Konut Finansmanı",
        "2,000,000 ₺ Konut + 140,000 ₺ Katılım Ücreti (Peşinatlı Model)",
        5,
        TwoPartners("Ortak 1", "Ortak 2"),
        new[]
        {
            Row("Konut Peşinatı (400.000 ₺) + Katılım Ücreti Peşinat (70.000 ₺)", 2026, 5, 15, 470000m),
            Row("Katılım Ücreti 2. Taksit (35.000 ₺) + 1. Tasarruf (35.000 ₺)", 2026, 6, 15, 70000m),
            Row("Katılım Ücreti 3. Taksit (35.000 ₺) + 2. Tasarruf (35.000 ₺)", 2026, 7, 15, 70000m),
            Row("3. Tasarruf Taksiti", 2026, 8, 15, 35000m),
            Row("4. Tasarruf Taksiti", 2026, 9, 15, 35000m),
            Row("5. Tasarruf Taksiti (Tahsisat Ayı)", 2026, 10, 15, 35000m),
            Row("6. Tasarruf Taksiti", 2026, 11, 15, 35000m),
            Row("7. Tasarruf Taksiti", 2026, 12, 15, 35000m),
            Row("8. Tasarruf Taksiti", 2027, 1, 15, 35000m),
            Row("9. Tasarruf Taksiti", 2027, 2, 15, 35000m),
            Row("10. Tasarruf Taksiti", 2027, 3, 15, 35000m),
            Row("11. Tasarruf Taksiti", 2027, 4, 15, 35000m)
        });

    private static PlanTemplateDefinition Sinpas() => new(
        "sinpas",
        "Sinpaş Yapı Tasarruf Konut Planı",
        "3,000,000 ₺ Konut Finansmanı + 180,000 ₺ Org. Ücreti (Eşit Taksitli Model)",
        6,
        TwoPartners("Ortak 1", "Ortak 2"),
        new[]
        {
            Row("Peşinat (500.000 ₺) + Org. Ücreti (180.000 ₺)", 2026, 5, 10, 680000m),
            Row("1. Tasarruf Taksiti", 2026, 6, 10, 50000m),
            Row("2. Tasarruf Taksiti", 2026, 7, 10, 50000m),
            Row("3. Tasarruf Taksiti", 2026, 8, 10, 50000m),
            Row("4. Tasarruf Taksiti", 2026, 9, 10, 50000m),
            Row("5. Tasarruf Taksiti", 2026, 10, 10, 50000m),
            Row("6. Tasarruf Taksiti (Tahsisat Ayı)", 2026, 11, 10, 50000m),
            Row("7. Tasarruf Taksiti", 2026, 12, 10, 50000m),
            Row("8. Tasarruf Taksiti", 2027, 1, 10, 50000m),
            Row("9. Tasarruf Taksiti", 2027, 2, 10, 50000m),
            Row("10. Tasarruf Taksiti", 2027, 3, 10, 50000m),
            Row("11. Tasarruf Taksiti", 2027, 4, 10, 50000m)
        });

    private static TemplatePartnerDef[] TwoPartners(string a, string b) =>
    [
        new TemplatePartnerDef(a, "#38bdf8", 50m),
        new TemplatePartnerDef(b, "#fb923c", 50m)
    ];

    private static TemplateInstallmentDef Row(
        string name, int y, int m, int d, decimal amount, bool paid = false) =>
        new(name, new DateOnly(y, m, d), amount, paid);
}
