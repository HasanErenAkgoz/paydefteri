using PayDefteri.Application.Common.Models;
using PayDefteri.Application.Partners;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PayDefteri.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/plans/{planId:guid}/partners")]
public sealed class PartnersController : ControllerBase
{
    private readonly ISender _sender;

    public PartnersController(ISender sender)
    {
        _sender = sender;
    }

    public sealed record PartnerRequest(
        string Name,
        string Color,
        decimal DefaultPct,
        int SortOrder,
        string? Iban,
        string? InviteEmail = null);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PartnerDto>>> List(Guid planId, CancellationToken ct)
        => Ok(await _sender.Send(new ListPartnersQuery(planId), ct));

    [HttpPost]
    public async Task<ActionResult<PartnerDto>> Create(Guid planId, [FromBody] PartnerRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new CreatePartnerCommand(
            planId, request.Name, request.Color, request.DefaultPct, request.SortOrder, request.Iban, request.InviteEmail), ct);
        return CreatedAtAction(nameof(List), new { planId }, result);
    }

    [HttpPut("{partnerId:guid}")]
    public async Task<ActionResult<PartnerDto>> Update(
        Guid planId, Guid partnerId, [FromBody] PartnerRequest request, CancellationToken ct)
        => Ok(await _sender.Send(new UpdatePartnerCommand(
            planId, partnerId, request.Name, request.Color, request.DefaultPct, request.SortOrder, request.Iban, request.InviteEmail), ct));

    [HttpDelete("{partnerId:guid}")]
    public async Task<IActionResult> Delete(Guid planId, Guid partnerId, CancellationToken ct)
    {
        await _sender.Send(new DeletePartnerCommand(planId, partnerId), ct);
        return NoContent();
    }
}
