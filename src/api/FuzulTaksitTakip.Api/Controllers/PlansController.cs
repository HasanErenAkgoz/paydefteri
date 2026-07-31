using FuzulTaksitTakip.Application.Common.Models;
using FuzulTaksitTakip.Application.Plans;
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

    public sealed record CreatePlanRequest(string Title, string Description);
    public sealed record UpdatePlanRequest(string Title, string Description, Guid? DeliveryInstallmentId);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlanDto>>> List(CancellationToken ct)
        => Ok(await _sender.Send(new ListPlansQuery(), ct));

    [HttpPost]
    public async Task<ActionResult<PlanDto>> Create([FromBody] CreatePlanRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new CreatePlanCommand(request.Title, request.Description ?? string.Empty), ct);
        return CreatedAtAction(nameof(Get), new { planId = result.Id }, result);
    }

    [HttpGet("{planId:guid}")]
    public async Task<ActionResult<PlanDto>> Get(Guid planId, CancellationToken ct)
        => Ok(await _sender.Send(new GetPlanQuery(planId), ct));

    [HttpPut("{planId:guid}")]
    public async Task<ActionResult<PlanDto>> Update(Guid planId, [FromBody] UpdatePlanRequest request, CancellationToken ct)
        => Ok(await _sender.Send(new UpdatePlanCommand(planId, request.Title, request.Description ?? string.Empty, request.DeliveryInstallmentId), ct));

    [HttpDelete("{planId:guid}")]
    public async Task<IActionResult> Delete(Guid planId, CancellationToken ct)
    {
        await _sender.Send(new DeletePlanCommand(planId), ct);
        return NoContent();
    }

    [HttpPost("{planId:guid}/seed/fuzul")]
    public async Task<ActionResult<PlanDto>> SeedFuzul(Guid planId, CancellationToken ct)
        => Ok(await _sender.Send(new SeedFuzulCommand(planId), ct));

    [HttpGet("{planId:guid}/dashboard")]
    public async Task<ActionResult<DashboardDto>> Dashboard(Guid planId, CancellationToken ct)
        => Ok(await _sender.Send(new GetDashboardQuery(planId), ct));

    [HttpGet("{planId:guid}/export")]
    public async Task<ActionResult<PlanExportDto>> Export(Guid planId, CancellationToken ct)
        => Ok(await _sender.Send(new ExportPlanQuery(planId), ct));

    [HttpPost("{planId:guid}/import")]
    public async Task<ActionResult<PlanDto>> Import(Guid planId, [FromBody] PlanExportDto data, CancellationToken ct)
        => Ok(await _sender.Send(new ImportPlanCommand(planId, data), ct));
}
