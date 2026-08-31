using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PayDefteri.Application.SpendingAnalysis;

namespace PayDefteri.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/spending-analysis")]
public sealed class SpendingAnalysisController : ControllerBase
{
    private readonly ISender _sender;

    public SpendingAnalysisController(ISender sender) => _sender = sender;

    public sealed record CategoryBody(string Category);
    public sealed record BudgetBody(decimal Amount, string Currency);
    public sealed record AskBody(string Question);

    [HttpGet("statements")]
    public async Task<ActionResult<IReadOnlyList<SpendingStatementSummaryDto>>> List(CancellationToken ct) =>
        Ok(await _sender.Send(new ListSpendingStatementsQuery(), ct));

    [HttpGet("statements/{statementId:guid}")]
    public async Task<ActionResult<SpendingStatementDetailDto>> Detail(Guid statementId, CancellationToken ct) =>
        Ok(await _sender.Send(new GetSpendingStatementQuery(statementId), ct));

    [HttpPost("statements")]
    [EnableRateLimiting("receipt-analysis")]
    [RequestSizeLimit(16 * 1024 * 1024)]
    public async Task<ActionResult<SpendingStatementDetailDto>> Upload(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Dosya gerekli",
                Detail = "CSV, XLSX veya metin tabanlı PDF ekstre dosyası seçin.",
                Status = StatusCodes.Status400BadRequest,
            });
        }
        if (file.Length > UploadSpendingStatementCommandValidator.MaxFileSize)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Dosya çok büyük",
                Detail = "Ekstre dosyası en fazla 15 MB olabilir.",
                Status = StatusCodes.Status400BadRequest,
            });
        }

        await using var input = file.OpenReadStream();
        using var memory = new MemoryStream((int)file.Length);
        await input.CopyToAsync(memory, ct);
        var result = await _sender.Send(new UploadSpendingStatementCommand(
            file.FileName,
            file.ContentType ?? "application/octet-stream",
            memory.ToArray()), ct);
        return CreatedAtAction(nameof(Detail), new { statementId = result.Id }, result);
    }

    [HttpDelete("statements/{statementId:guid}")]
    public async Task<IActionResult> Delete(Guid statementId, CancellationToken ct)
    {
        await _sender.Send(new DeleteSpendingStatementCommand(statementId), ct);
        return NoContent();
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<string>>> Categories(CancellationToken ct) =>
        Ok(await _sender.Send(new ListSpendingCategoriesQuery(), ct));

    [HttpPatch("statements/{statementId:guid}/transactions/{transactionId:guid}/category")]
    public async Task<ActionResult<SpendingTransactionDto>> UpdateCategory(
        Guid statementId,
        Guid transactionId,
        [FromBody] CategoryBody body,
        CancellationToken ct) => Ok(await _sender.Send(new UpdateSpendingTransactionCategoryCommand(
            statementId,
            transactionId,
            body.Category), ct));

    [HttpGet("statements/{statementId:guid}/dashboard")]
    public async Task<ActionResult<SpendingDashboardDto>> Dashboard(Guid statementId, CancellationToken ct) =>
        Ok(await _sender.Send(new GetSpendingDashboardQuery(statementId), ct));

    [HttpGet("statements/{statementId:guid}/patterns")]
    public async Task<ActionResult<SpendingPatternsDto>> Patterns(Guid statementId, CancellationToken ct) =>
        Ok(await _sender.Send(new GetSpendingPatternsQuery(statementId), ct));

    [HttpPost("statements/{statementId:guid}/coach")]
    [EnableRateLimiting("spending-coach")]
    public async Task<ActionResult<SpendingCoachResponseDto>> Coach(Guid statementId, CancellationToken ct) =>
        Ok(await _sender.Send(new GetSpendingCoachQuery(statementId), ct));

    [HttpPost("statements/{statementId:guid}/ask")]
    [EnableRateLimiting("spending-coach")]
    public async Task<ActionResult<SpendingCoachResponseDto>> Ask(
        Guid statementId, [FromBody] AskBody body, CancellationToken ct) =>
        Ok(await _sender.Send(new AskSpendingCoachQuery(statementId, body.Question), ct));

    [HttpGet("budgets/{month}")]
    public async Task<ActionResult<IReadOnlyList<SpendingBudgetDto>>> Budgets(string month, CancellationToken ct) =>
        Ok(await _sender.Send(new ListSpendingBudgetsQuery(ParseMonth(month)), ct));

    [HttpPut("budgets/{month}/{category}")]
    public async Task<ActionResult<SpendingBudgetDto>> UpsertBudget(
        string month, string category, [FromBody] BudgetBody body, CancellationToken ct) =>
        Ok(await _sender.Send(new UpsertSpendingBudgetCommand(ParseMonth(month), category, body.Currency.ToUpperInvariant(), body.Amount), ct));

    [HttpDelete("budgets/{month}/{category}")]
    public async Task<IActionResult> DeleteBudget(string month, string category, [FromQuery] string currency = "TRY", CancellationToken ct = default)
    {
        await _sender.Send(new DeleteSpendingBudgetCommand(ParseMonth(month), category, currency.ToUpperInvariant()), ct);
        return NoContent();
    }

    [HttpGet("statements/{statementId:guid}/budget-status")]
    public async Task<ActionResult<SpendingBudgetStatusDto>> BudgetStatus(Guid statementId, CancellationToken ct) =>
        Ok(await _sender.Send(new GetSpendingBudgetStatusQuery(statementId), ct));

    private static DateOnly ParseMonth(string month) =>
        DateOnly.TryParseExact(month + "-01", "yyyy-MM-dd", out var value)
            ? value
            : throw new BadHttpRequestException("Ay yyyy-MM biçiminde olmalıdır.");
}
