using PayDefteri.Application.Common.Models;
using PayDefteri.Application.Reminders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PayDefteri.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reminders")]
public sealed class RemindersController : ControllerBase
{
    private readonly ISender _sender;

    public RemindersController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Manual trigger for smoke tests / ops. Runs the same job as the daily hosted service.</summary>
    [HttpPost("process")]
    public async Task<ActionResult<ProcessPaymentRemindersResult>> Process(CancellationToken ct)
        => Ok(await _sender.Send(new ProcessPaymentRemindersCommand(), ct));
}
