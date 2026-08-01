using FuzulTaksitTakip.Application.Common.Models;
using FuzulTaksitTakip.Application.Membership;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuzulTaksitTakip.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/plans/{planId:guid}")]
public sealed class MembershipController : ControllerBase
{
    private readonly ISender _sender;

    public MembershipController(ISender sender)
    {
        _sender = sender;
    }

    public sealed record InviteRequest(string Email, Guid PartnerId);

    [HttpGet("members")]
    public async Task<ActionResult<IReadOnlyList<PlanMemberDto>>> Members(Guid planId, CancellationToken ct)
        => Ok(await _sender.Send(new ListMembersQuery(planId), ct));

    [HttpGet("invites")]
    public async Task<ActionResult<IReadOnlyList<PlanInviteDto>>> Invites(Guid planId, CancellationToken ct)
        => Ok(await _sender.Send(new ListInvitesQuery(planId), ct));

    [HttpPost("invites")]
    public async Task<ActionResult<PlanInviteDto>> Invite(Guid planId, [FromBody] InviteRequest request, CancellationToken ct)
        => Ok(await _sender.Send(new CreateInviteCommand(planId, request.Email, request.PartnerId), ct));

    [HttpDelete("invites/{inviteId:guid}")]
    public async Task<IActionResult> Revoke(Guid planId, Guid inviteId, CancellationToken ct)
    {
        await _sender.Send(new RevokeInviteCommand(planId, inviteId), ct);
        return NoContent();
    }

    [HttpPost("invites/{inviteId:guid}/resend")]
    public async Task<ActionResult<PlanInviteDto>> Resend(Guid planId, Guid inviteId, CancellationToken ct)
        => Ok(await _sender.Send(new ResendInviteCommand(planId, inviteId), ct));

    public sealed record LinkSelfRequest(Guid PartnerId);

    [HttpPost("link-self")]
    public async Task<ActionResult<PartnerDto>> LinkSelf(Guid planId, [FromBody] LinkSelfRequest request, CancellationToken ct)
        => Ok(await _sender.Send(new LinkSelfToPartnerCommand(planId, request.PartnerId), ct));
}

[ApiController]
[Authorize]
[Route("api/invites")]
public sealed class InvitesController : ControllerBase
{
    private readonly ISender _sender;

    public InvitesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<PlanInviteDto>>> Mine(CancellationToken ct)
        => Ok(await _sender.Send(new ListMyPendingInvitesQuery(), ct));

    [AllowAnonymous]
    [HttpGet("{token}/preview")]
    public async Task<ActionResult<InvitePreviewDto>> Preview(string token, CancellationToken ct)
        => Ok(await _sender.Send(new GetInvitePreviewQuery(token), ct));

    [HttpPost("{token}/accept")]
    public async Task<ActionResult<PlanDto>> Accept(string token, CancellationToken ct)
        => Ok(await _sender.Send(new AcceptInviteCommand(token), ct));
}
