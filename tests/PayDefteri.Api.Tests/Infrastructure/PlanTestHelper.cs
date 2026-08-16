using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace PayDefteri.Api.Tests.Infrastructure;

/// <summary>Shared builders for abidik-gubidik API scenarios.</summary>
public static class PlanTestHelper
{
    public const string ValidTrIbanSpaced = "TR33 0006 1005 1978 6457 8413 26";
    public const string ValidTrIban = "TR330006100519786457841326";

    public static async Task<(PlanDto Plan, List<PartnerDto> Partners)> CreatePlanWithPartnersAsync(
        TestClient api,
        string? title = null,
        int partnerCount = 2)
    {
        var (create, plan) = await api.PostAsync<PlanDto>("/api/plans", new
        {
            title = title ?? $"Edge {Guid.NewGuid():N}",
            description = "edge",
        });
        create.EnsureSuccessStatusCode();
        (await api.PostEmptyAsync($"/api/plans/{plan!.Id}/seed/empty")).EnsureSuccessStatusCode();

        var pct = partnerCount == 0 ? 0m : Math.Round(100m / partnerCount, 2);
        var partners = new List<PartnerDto>();
        for (var i = 0; i < partnerCount; i++)
        {
            var defaultPct = i == partnerCount - 1
                ? 100m - pct * (partnerCount - 1)
                : pct;
            var (res, partner) = await api.PostAsync<PartnerDto>($"/api/plans/{plan.Id}/partners", new
            {
                name = $"P{i + 1}",
                color = i % 2 == 0 ? "#38bdf8" : "#fb923c",
                defaultPct,
                sortOrder = i + 1,
                iban = (string?)null,
            });
            res.EnsureSuccessStatusCode();
            partners.Add(partner!);
        }

        return (plan, partners);
    }

    public static async Task<InstallmentDto> CreateInstallmentAsync(
        TestClient api,
        Guid planId,
        string name = "T1",
        decimal total = 1000m,
        string shareType = "Default",
        int sortOrder = 1,
        object? customShares = null,
        string? dueDate = null)
    {
        // Default to current month so payment marking is allowed by future-month rules.
        dueDate ??= new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).ToString("yyyy-MM-dd");
        var (res, inst) = await api.PostAsync<InstallmentDto>(
            $"/api/plans/{planId}/installments",
            new
            {
                name,
                dueDate,
                totalAmount = total,
                shareType,
                sortOrder,
                customShares,
            });
        res.EnsureSuccessStatusCode();
        return inst!;
    }

    public static object DefaultPlanUpdate(PlanDto plan, object? overrides = null)
    {
        // Base update payload matching PlansController.UpdatePlanRequest
        return new
        {
            title = plan.Title,
            description = plan.Description ?? "",
            deliveryInstallmentId = plan.DeliveryInstallmentId,
            requireReceipt = false,
            ibanMode = "None",
            settlementIban = (string?)null,
            remindersEnabled = false,
            reminderDaysBefore = Array.Empty<int>(),
            reminderDaysAfter = Array.Empty<int>(),
        };
    }

    public static async Task<HttpResponseMessage> UpdatePlanRawAsync(
        TestClient api,
        Guid planId,
        object body) =>
        (await api.PutAsync<object>($"/api/plans/{planId}", body)).Response;

    public static async Task<HttpResponseMessage> UploadTinyPdfReceiptAsync(
        HttpClient http,
        Guid planId,
        Guid installmentId,
        Guid partnerId,
        string fileName = "dekont.pdf",
        string contentType = "application/pdf",
        byte[]? bytes = null)
    {
        bytes ??= Encoding.ASCII.GetBytes("%PDF-1.4 tiny");
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);
        return await http.PostAsync(
            $"/api/plans/{planId}/installments/{installmentId}/payments/{partnerId}/receipt",
            form);
    }

    public static PlanExportDto MinimalExport(
        string title = "Import Edge",
        decimal pctA = 50m,
        decimal pctB = 50m)
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var inst = Guid.NewGuid();
        return new PlanExportDto(
            title,
            "desc",
            null,
            new[]
            {
                new PartnerExportDto(a, "A", "#111111", pctA, 1),
                new PartnerExportDto(b, "B", "#222222", pctB, 2),
            },
            new[]
            {
                new InstallmentExportDto(
                    inst,
                    "T1",
                    new DateOnly(2026, 9, 1),
                    100m,
                    "Default",
                    1,
                    Array.Empty<CustomShareExportDto>(),
                    Array.Empty<PaymentExportDto>()),
            });
    }
}

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

public sealed record CustomShareExportDto(Guid PartnerId, decimal Amount);

public sealed record PaymentExportDto(
    Guid PartnerId,
    bool IsPaid,
    DateOnly? PaidAt,
    Guid? PaidByPartnerId,
    string Note);

public sealed record InstallmentExportDto(
    Guid Id,
    string Name,
    DateOnly DueDate,
    decimal TotalAmount,
    string ShareType,
    int SortOrder,
    IReadOnlyList<CustomShareExportDto> CustomShares,
    IReadOnlyList<PaymentExportDto> Payments);

public sealed record InvitePreviewDto(
    string PlanTitle,
    string PartnerName,
    string Email,
    bool IsAcceptable,
    string? Status);

public sealed record DashboardDto(
    Guid PlanId,
    string Title,
    IReadOnlyList<object>? Settlements);
