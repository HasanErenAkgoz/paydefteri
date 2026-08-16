using FuzulTaksitTakip.Application.Common.Models;
using FuzulTaksitTakip.Application.Expenses;
using FuzulTaksitTakip.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FuzulTaksitTakip.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/plans/{planId:guid}/expenses")]
public sealed class ExpensesController : ControllerBase
{
    private readonly ISender _sender;

    public ExpensesController(ISender sender)
    {
        _sender = sender;
    }

    public sealed record ExpenseBody(
        string Name,
        DateOnly OccurredOn,
        decimal TotalAmount,
        ShareType ShareType,
        ExpenseStatus Status,
        Guid? PaidByPartnerId,
        Guid? CategoryId,
        string? Note,
        List<CustomShareDto>? CustomShares,
        List<ExpensePaymentDto>? Payments,
        int InstallmentCount = 1);

    public sealed record CategoryBody(string Name, string? Color);

    public sealed record TransferBody(
        Guid FromPartnerId,
        Guid ToPartnerId,
        decimal Amount,
        DateOnly TransferredOn,
        string? Note);

    public sealed record RecurrenceBody(
        string Name,
        decimal TotalAmount,
        ShareType ShareType,
        Guid? CategoryId,
        Guid? DefaultPaidByPartnerId,
        RecurrenceFrequency Frequency,
        int AnchorDay,
        DateOnly StartDate,
        DateOnly? EndDate,
        string? Note,
        List<CustomShareDto>? CustomShares);

    [HttpGet("board")]
    public async Task<ActionResult<ExpenseBoardDto>> Board(Guid planId, CancellationToken ct)
        => Ok(await _sender.Send(new GetExpenseBoardQuery(planId), ct));

    [HttpGet]
    public async Task<ActionResult<PagedExpenseDto>> List(
        Guid planId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
        => Ok(await _sender.Send(new ListExpensesQuery(planId, page, pageSize), ct));

    [HttpPost]
    public async Task<ActionResult<ExpenseDto>> Create(Guid planId, [FromBody] ExpenseBody body, CancellationToken ct)
    {
        var result = await _sender.Send(new CreateExpenseCommand(
            planId,
            body.Name,
            body.OccurredOn,
            body.TotalAmount,
            body.ShareType,
            body.Status,
            body.PaidByPartnerId,
            body.CategoryId,
            body.Note,
            body.CustomShares,
            body.Payments,
            body.InstallmentCount), ct);
        return CreatedAtAction(nameof(Board), new { planId }, result);
    }

    [HttpPost("analyze-receipt")]
    [EnableRateLimiting("receipt-analysis")]
    [RequestSizeLimit(8 * 1024 * 1024)]
    public async Task<ActionResult<ExpenseReceiptDraftDto>> AnalyzeReceipt(
        Guid planId,
        IFormFile? file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Görsel gerekli",
                Detail = "Fiş veya fatura görseli seçin.",
                Status = StatusCodes.Status400BadRequest,
            });
        }

        await using var stream = file.OpenReadStream();
        using var memory = new MemoryStream((int)file.Length);
        await stream.CopyToAsync(memory, ct);
        return Ok(await _sender.Send(
            new AnalyzeExpenseReceiptCommand(planId, file.ContentType, memory.ToArray()),
            ct));
    }

    [HttpPut("{expenseId:guid}")]
    public async Task<ActionResult<ExpenseDto>> Update(
        Guid planId,
        Guid expenseId,
        [FromBody] ExpenseBody body,
        CancellationToken ct)
        => Ok(await _sender.Send(new UpdateExpenseCommand(
            planId,
            expenseId,
            body.Name,
            body.OccurredOn,
            body.TotalAmount,
            body.ShareType,
            body.Status,
            body.PaidByPartnerId,
            body.CategoryId,
            body.Note,
            body.CustomShares,
            body.Payments), ct));

    [HttpDelete("{expenseId:guid}")]
    public async Task<IActionResult> Delete(Guid planId, Guid expenseId, CancellationToken ct)
    {
        await _sender.Send(new DeleteExpenseCommand(planId, expenseId), ct);
        return NoContent();
    }

    [HttpPost("categories")]
    public async Task<ActionResult<ExpenseCategoryDto>> CreateCategory(
        Guid planId,
        [FromBody] CategoryBody body,
        CancellationToken ct)
        => Ok(await _sender.Send(new CreateExpenseCategoryCommand(planId, body.Name, body.Color ?? "#94a3b8"), ct));

    [HttpPost("transfers")]
    public async Task<ActionResult<SettlementTransferDto>> CreateTransfer(
        Guid planId,
        [FromBody] TransferBody body,
        CancellationToken ct)
        => Ok(await _sender.Send(new CreateSettlementTransferCommand(
            planId,
            body.FromPartnerId,
            body.ToPartnerId,
            body.Amount,
            body.TransferredOn,
            body.Note), ct));

    [HttpDelete("transfers/{transferId:guid}")]
    public async Task<IActionResult> DeleteTransfer(Guid planId, Guid transferId, CancellationToken ct)
    {
        await _sender.Send(new DeleteSettlementTransferCommand(planId, transferId), ct);
        return NoContent();
    }

    [HttpPost("recurrences")]
    public async Task<ActionResult<ExpenseRecurrenceDto>> CreateRecurrence(
        Guid planId,
        [FromBody] RecurrenceBody body,
        CancellationToken ct)
        => Ok(await _sender.Send(new CreateExpenseRecurrenceCommand(
            planId,
            body.Name,
            body.TotalAmount,
            body.ShareType,
            body.CategoryId,
            body.DefaultPaidByPartnerId,
            body.Frequency,
            body.AnchorDay,
            body.StartDate,
            body.EndDate,
            body.Note,
            body.CustomShares), ct));

    [HttpDelete("recurrences/{recurrenceId:guid}")]
    public async Task<IActionResult> DeleteRecurrence(Guid planId, Guid recurrenceId, CancellationToken ct)
    {
        await _sender.Send(new DeleteExpenseRecurrenceCommand(planId, recurrenceId), ct);
        return NoContent();
    }
}
