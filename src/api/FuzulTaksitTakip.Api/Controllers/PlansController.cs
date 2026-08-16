using FuzulTaksitTakip.Application.Common.Models;
using FuzulTaksitTakip.Application.Plans;
using FuzulTaksitTakip.Application.Reminders;
using FuzulTaksitTakip.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuzulTaksitTakip.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/plans")]
public sealed class PlansController : ControllerBase
{
    private readonly ISender _sender;

    public PlansController(ISender sender)
    {
        _sender = sender;
    }

    public sealed record CreatePlanRequest(string Title, string Description, PlanType PlanType = PlanType.Installment);

    public sealed record UpdatePlanRequest(
        string Title,
        string Description,
        Guid? DeliveryInstallmentId,
        bool RequireReceipt,
        IbanMode IbanMode,
        string? SettlementIban,
        bool RemindersEnabled,
        int[]? ReminderDaysBefore,
        int[]? ReminderDaysAfter);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlanDto>>> List(
        [FromQuery] bool includeArchived = false,
        CancellationToken ct = default)
        => Ok(await _sender.Send(new ListPlansQuery(includeArchived), ct));

    [HttpPost]
    public async Task<ActionResult<PlanDto>> Create([FromBody] CreatePlanRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(
            new CreatePlanCommand(request.Title, request.Description ?? string.Empty, request.PlanType), ct);
        return CreatedAtAction(nameof(Get), new { planId = result.Id }, result);
    }

    [HttpGet("{planId:guid}")]
    public async Task<ActionResult<PlanDto>> Get(Guid planId, CancellationToken ct)
        => Ok(await _sender.Send(new GetPlanQuery(planId), ct));

    [HttpPut("{planId:guid}")]
    public async Task<ActionResult<PlanDto>> Update(Guid planId, [FromBody] UpdatePlanRequest request, CancellationToken ct)
        => Ok(await _sender.Send(new UpdatePlanCommand(
            planId,
            request.Title,
            request.Description ?? string.Empty,
            request.DeliveryInstallmentId,
            request.RequireReceipt,
            request.IbanMode,
            request.SettlementIban,
            request.RemindersEnabled,
            request.ReminderDaysBefore ?? Array.Empty<int>(),
            request.ReminderDaysAfter ?? Array.Empty<int>()), ct));

    [HttpDelete("{planId:guid}")]
    public async Task<IActionResult> Delete(Guid planId, CancellationToken ct)
    {
        await _sender.Send(new DeletePlanCommand(planId), ct);
        return NoContent();
    }

    [HttpPost("{planId:guid}/archive")]
    public async Task<IActionResult> Archive(Guid planId, CancellationToken ct)
    {
        await _sender.Send(new ArchivePlanCommand(planId), ct);
        return NoContent();
    }

    [HttpPost("{planId:guid}/restore")]
    public async Task<ActionResult<PlanDto>> Restore(Guid planId, CancellationToken ct)
        => Ok(await _sender.Send(new RestorePlanCommand(planId), ct));

    [HttpPost("{planId:guid}/copy")]
    public async Task<ActionResult<PlanDto>> Copy(Guid planId, CancellationToken ct)
    {
        var result = await _sender.Send(new CopyPlanCommand(planId), ct);
        return CreatedAtAction(nameof(Get), new { planId = result.Id }, result);
    }

    [HttpPost("{planId:guid}/seed/fuzul")]
    public async Task<ActionResult<PlanDto>> SeedFuzul(Guid planId, CancellationToken ct)
        => Ok(await _sender.Send(new SeedPlanTemplateCommand(planId, "fuzul"), ct));

    [HttpPost("{planId:guid}/seed/{templateKey}")]
    public async Task<ActionResult<PlanDto>> SeedTemplate(
        Guid planId,
        string templateKey,
        [FromBody] SeedTemplateBody? body,
        CancellationToken ct)
        => Ok(await _sender.Send(new SeedPlanTemplateCommand(planId, templateKey, body), ct));

    [HttpPost("{planId:guid}/settle-up")]
    public async Task<IActionResult> SettleUp(Guid planId, CancellationToken ct)
    {
        await _sender.Send(new SettleUpCommand(planId), ct);
        return NoContent();
    }

    [HttpGet("{planId:guid}/dashboard")]
    public async Task<ActionResult<DashboardDto>> Dashboard(Guid planId, CancellationToken ct)
        => Ok(await _sender.Send(new GetDashboardQuery(planId), ct));

    [HttpGet("{planId:guid}/report-summary")]
    public async Task<ActionResult<ReportSummaryDto>> ReportSummary(Guid planId, CancellationToken ct)
        => Ok(await _sender.Send(new GetReportSummaryQuery(planId), ct));

    [HttpGet("{planId:guid}/reminders")]
    public async Task<ActionResult<IReadOnlyList<ReminderHistoryItemDto>>> Reminders(Guid planId, CancellationToken ct)
        => Ok(await _sender.Send(new ListPlanRemindersQuery(planId), ct));

    [HttpGet("{planId:guid}/activity")]
    public async Task<ActionResult<IReadOnlyList<PlanActivityItemDto>>> Activity(Guid planId, CancellationToken ct)
        => Ok(await _sender.Send(new ListPlanActivityQuery(planId), ct));

    [HttpGet("{planId:guid}/export")]
    public async Task<ActionResult<PlanExportDto>> Export(Guid planId, CancellationToken ct)
        => Ok(await _sender.Send(new ExportPlanQuery(planId), ct));

    [HttpPost("{planId:guid}/import")]
    public async Task<ActionResult<PlanDto>> Import(Guid planId, [FromBody] PlanExportDto data, CancellationToken ct)
        => Ok(await _sender.Send(new ImportPlanCommand(planId, data), ct));

    [HttpPost("{planId:guid}/parse-document")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<ActionResult<PlanDocumentPreviewDto>> ParseDocument(
        Guid planId,
        IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { detail = "Dosya seçilmedi." });
        }

        await using var stream = file.OpenReadStream();
        var result = await _sender.Send(
            new ParsePlanDocumentCommand(planId, stream, file.FileName, file.ContentType ?? "application/octet-stream"),
            ct);
        return Ok(result);
    }
}
