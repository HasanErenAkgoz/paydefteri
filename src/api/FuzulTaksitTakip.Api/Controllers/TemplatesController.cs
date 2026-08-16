using FuzulTaksitTakip.Application.Plans;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuzulTaksitTakip.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/templates")]
public sealed class TemplatesController : ControllerBase
{
    private readonly ISender _sender;

    public TemplatesController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TemplateListItemDto>>> List(CancellationToken ct)
        => Ok(await _sender.Send(new ListTemplateKeysQuery(), ct));

    [HttpGet("{templateKey}")]
    public async Task<ActionResult<PlanTemplatePreviewDto>> Preview(string templateKey, CancellationToken ct)
        => Ok(await _sender.Send(new GetTemplatePreviewQuery(templateKey), ct));
}
