using PayDefteri.Application.Common.Models;
using PayDefteri.Application.Installments;
using PayDefteri.Application.Payments;
using PayDefteri.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PayDefteri.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/plans/{planId:guid}/installments")]
public sealed class InstallmentsController : ControllerBase
{
    private readonly ISender _sender;

    public InstallmentsController(ISender sender)
    {
        _sender = sender;
    }

    public sealed record InstallmentRequest(
        string Name,
        DateOnly DueDate,
        decimal TotalAmount,
        ShareType ShareType,
        int SortOrder,
        List<CustomShareDto>? CustomShares);

    public sealed record PaymentRequest(
        bool IsPaid,
        DateOnly? PaidAt,
        Guid? PaidByPartnerId,
        string? Note);

    public sealed record BulkIncreaseRequest(Guid FromInstallmentId, BulkIncreaseType Type, decimal Value);
    public sealed record RejectPaymentRequest(string? Note);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InstallmentDto>>> List(Guid planId, CancellationToken ct)
        => Ok(await _sender.Send(new ListInstallmentsQuery(planId), ct));

    [HttpPost]
    public async Task<ActionResult<InstallmentDto>> Create(Guid planId, [FromBody] InstallmentRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new CreateInstallmentCommand(
            planId,
            request.Name,
            request.DueDate,
            request.TotalAmount,
            request.ShareType,
            request.SortOrder,
            request.CustomShares), ct);
        return CreatedAtAction(nameof(List), new { planId }, result);
    }

    [HttpPut("{installmentId:guid}")]
    public async Task<ActionResult<InstallmentDto>> Update(
        Guid planId, Guid installmentId, [FromBody] InstallmentRequest request, CancellationToken ct)
        => Ok(await _sender.Send(new UpdateInstallmentCommand(
            planId,
            installmentId,
            request.Name,
            request.DueDate,
            request.TotalAmount,
            request.ShareType,
            request.SortOrder,
            request.CustomShares), ct));

    [HttpDelete("{installmentId:guid}")]
    public async Task<IActionResult> Delete(Guid planId, Guid installmentId, CancellationToken ct)
    {
        await _sender.Send(new DeleteInstallmentCommand(planId, installmentId), ct);
        return NoContent();
    }

    [HttpPut("{installmentId:guid}/payments/{partnerId:guid}")]
    public async Task<ActionResult<PaymentDto>> UpsertPayment(
        Guid planId,
        Guid installmentId,
        Guid partnerId,
        [FromBody] PaymentRequest request,
        CancellationToken ct)
        => Ok(await _sender.Send(new UpsertPaymentCommand(
            planId,
            installmentId,
            partnerId,
            request.IsPaid,
            request.PaidAt,
            request.PaidByPartnerId,
            request.Note), ct));

    [HttpPost("{installmentId:guid}/payments/{partnerId:guid}/receipt")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<PaymentDto>> UploadReceipt(
        Guid planId,
        Guid installmentId,
        Guid partnerId,
        IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { detail = "Dekont dosyası gerekli." });
        }

        await using var stream = file.OpenReadStream();
        var result = await _sender.Send(new UploadReceiptCommand(
            planId,
            installmentId,
            partnerId,
            stream,
            file.ContentType,
            file.FileName), ct);
        return Ok(result);
    }

    [HttpPost("{installmentId:guid}/payments/{partnerId:guid}/approve")]
    public async Task<ActionResult<PaymentDto>> ApprovePayment(
        Guid planId, Guid installmentId, Guid partnerId, CancellationToken ct)
        => Ok(await _sender.Send(new ApprovePaymentCommand(planId, installmentId, partnerId), ct));

    [HttpPost("{installmentId:guid}/payments/{partnerId:guid}/reject")]
    public async Task<ActionResult<PaymentDto>> RejectPayment(
        Guid planId,
        Guid installmentId,
        Guid partnerId,
        [FromBody] RejectPaymentRequest? body,
        CancellationToken ct)
        => Ok(await _sender.Send(new RejectPaymentCommand(planId, installmentId, partnerId, body?.Note), ct));

    [HttpPost("bulk-increase")]
    public async Task<ActionResult<IReadOnlyList<InstallmentDto>>> BulkIncrease(
        Guid planId, [FromBody] BulkIncreaseRequest request, CancellationToken ct)
        => Ok(await _sender.Send(new BulkIncreaseCommand(
            planId, request.FromInstallmentId, request.Type, request.Value), ct));
}
