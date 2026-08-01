using FluentValidation;
using FuzulTaksitTakip.Application.Common.Exceptions;
using FuzulTaksitTakip.Application.Common.Interfaces;
using FuzulTaksitTakip.Application.Common.Mapping;
using FuzulTaksitTakip.Application.Common.Models;
using FuzulTaksitTakip.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FuzulTaksitTakip.Application.Payments;

public sealed record UploadReceiptCommand(
    Guid PlanId,
    Guid InstallmentId,
    Guid PartnerId,
    Stream Content,
    string ContentType,
    string FileName) : IRequest<PaymentDto>;

public sealed class UploadReceiptCommandHandler : IRequestHandler<UploadReceiptCommand, PaymentDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;
    private readonly IReceiptStorage _storage;

    public UploadReceiptCommandHandler(IAppDbContext db, IPlanAuthorization auth, IReceiptStorage storage)
    {
        _db = db;
        _auth = auth;
        _storage = storage;
    }

    public async Task<PaymentDto> Handle(UploadReceiptCommand request, CancellationToken cancellationToken)
    {
        await _auth.EnsureCanMarkPaymentAsync(request.PlanId, request.PartnerId, cancellationToken);

        var installmentExists = await _db.Installments.AnyAsync(
            i => i.Id == request.InstallmentId && i.PlanId == request.PlanId,
            cancellationToken);
        if (!installmentExists)
        {
            throw new NotFoundException(nameof(Installment), request.InstallmentId);
        }

        var partnerExists = await _db.Partners.AnyAsync(
            p => p.Id == request.PartnerId && p.PlanId == request.PlanId,
            cancellationToken);
        if (!partnerExists)
        {
            throw new NotFoundException(nameof(Partner), request.PartnerId);
        }

        var payment = await _db.Payments.FirstOrDefaultAsync(
            p => p.InstallmentId == request.InstallmentId && p.PartnerId == request.PartnerId,
            cancellationToken);

        if (payment is null)
        {
            payment = new Payment
            {
                InstallmentId = request.InstallmentId,
                PartnerId = request.PartnerId
            };
            _db.Payments.Add(payment);
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (!string.IsNullOrEmpty(payment.ReceiptStorageKey))
        {
            await _storage.DeleteAsync(payment.ReceiptStorageKey, cancellationToken);
        }

        string storageKey;
        try
        {
            storageKey = await _storage.SaveAsync(
                request.PlanId,
                payment.Id,
                request.Content,
                request.ContentType,
                request.FileName,
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new ValidationException(ex.Message);
        }

        payment.ReceiptStorageKey = storageKey;
        payment.ReceiptContentType = request.ContentType;
        payment.ReceiptFileName = Path.GetFileName(request.FileName);
        payment.ReceiptUploadedAtUtc = DateTime.UtcNow;
        payment.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return payment.ToDto();
    }
}
