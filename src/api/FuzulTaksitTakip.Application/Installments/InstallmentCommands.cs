using FluentValidation;
using FuzulTaksitTakip.Application.Common;
using FuzulTaksitTakip.Application.Common.Exceptions;
using FuzulTaksitTakip.Application.Common.Interfaces;
using FuzulTaksitTakip.Application.Common.Mapping;
using FuzulTaksitTakip.Application.Common.Models;
using FuzulTaksitTakip.Domain.Entities;
using FuzulTaksitTakip.Domain.Enums;
using FuzulTaksitTakip.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FuzulTaksitTakip.Application.Installments;

public sealed record ListInstallmentsQuery(Guid PlanId) : IRequest<IReadOnlyList<InstallmentDto>>;

public sealed class ListInstallmentsQueryHandler : IRequestHandler<ListInstallmentsQuery, IReadOnlyList<InstallmentDto>>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;

    public ListInstallmentsQueryHandler(IAppDbContext db, IPlanAuthorization auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task<IReadOnlyList<InstallmentDto>> Handle(ListInstallmentsQuery request, CancellationToken cancellationToken)
    {
        await _auth.EnsureMemberAsync(request.PlanId, cancellationToken);

        var list = await _db.Installments.AsNoTracking()
            .Include(i => i.CustomShares)
            .Include(i => i.Payments)
            .Where(i => i.PlanId == request.PlanId)
            .OrderBy(i => i.SortOrder)
            .ToListAsync(cancellationToken);

        return list.Select(Map).ToList();
    }

    internal static InstallmentDto Map(Installment i) => new(
        i.Id,
        i.PlanId,
        i.Name,
        i.DueDate,
        i.TotalAmount,
        i.ShareType,
        i.SortOrder,
        i.CustomShares.Select(s => new CustomShareDto(s.PartnerId, s.Amount)).ToList(),
        i.Payments.Select(p => new PaymentDto(
            p.PartnerId, p.IsPaid, p.PaidAt, p.PaidByPartnerId, p.Note,
            !string.IsNullOrEmpty(p.ReceiptStorageKey), p.ReviewStatus)).ToList());
}

public sealed record CreateInstallmentCommand(
    Guid PlanId,
    string Name,
    DateOnly DueDate,
    decimal TotalAmount,
    ShareType ShareType,
    int SortOrder,
    IReadOnlyList<CustomShareDto>? CustomShares) : IRequest<InstallmentDto>;

public sealed class CreateInstallmentCommandValidator : AbstractValidator<CreateInstallmentCommand>
{
    public CreateInstallmentCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.TotalAmount).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.ShareType).IsInEnum();
    }
}

public sealed class CreateInstallmentCommandHandler : IRequestHandler<CreateInstallmentCommand, InstallmentDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;
    private readonly ICurrentUser _currentUser;

    public CreateInstallmentCommandHandler(IAppDbContext db, IPlanAuthorization auth, ICurrentUser currentUser)
    {
        _db = db;
        _auth = auth;
        _currentUser = currentUser;
    }

    public async Task<InstallmentDto> Handle(CreateInstallmentCommand request, CancellationToken cancellationToken)
    {
        await _auth.EnsureOwnerAsync(request.PlanId, cancellationToken);

        var installment = new Installment
        {
            PlanId = request.PlanId,
            Name = request.Name.Trim(),
            DueDate = request.DueDate,
            TotalAmount = request.TotalAmount,
            ShareType = request.ShareType,
            SortOrder = request.SortOrder
        };

        ApplyCustomShares(installment, request.CustomShares);
        ValidateCustom(installment);

        _db.Installments.Add(installment);
        PlanActivity.Write(_db, _currentUser, request.PlanId, PlanActivityType.InstallmentCreated, $"Taksit eklendi: {installment.Name}");
        await _db.SaveChangesAsync(cancellationToken);

        return ListInstallmentsQueryHandler.Map(installment);
    }

    internal static void ApplyCustomShares(Installment installment, IReadOnlyList<CustomShareDto>? shares)
    {
        installment.CustomShares.Clear();
        if (shares is null)
        {
            return;
        }

        foreach (var s in shares)
        {
            installment.CustomShares.Add(new InstallmentShare
            {
                InstallmentId = installment.Id,
                PartnerId = s.PartnerId,
                Amount = s.Amount
            });
        }
    }

    internal static void ValidateCustom(Installment installment)
    {
        if (installment.ShareType != ShareType.Custom)
        {
            return;
        }

        if (!ShareCalculator.CustomSharesMatchTotal(installment))
        {
            var sum = installment.CustomShares.Sum(s => s.Amount);
            throw new ValidationException(
                $"Özel payların toplamı taksit tutarına eşit olmalı. Paylar: {sum:N2} ₺, taksit: {installment.TotalAmount:N2} ₺.");
        }
    }
}

public sealed record UpdateInstallmentCommand(
    Guid PlanId,
    Guid InstallmentId,
    string Name,
    DateOnly DueDate,
    decimal TotalAmount,
    ShareType ShareType,
    int SortOrder,
    IReadOnlyList<CustomShareDto>? CustomShares) : IRequest<InstallmentDto>;

public sealed class UpdateInstallmentCommandValidator : AbstractValidator<UpdateInstallmentCommand>
{
    public UpdateInstallmentCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.TotalAmount).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.ShareType).IsInEnum();
    }
}

public sealed class UpdateInstallmentCommandHandler : IRequestHandler<UpdateInstallmentCommand, InstallmentDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;

    public UpdateInstallmentCommandHandler(IAppDbContext db, IPlanAuthorization auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task<InstallmentDto> Handle(UpdateInstallmentCommand request, CancellationToken cancellationToken)
    {
        await _auth.EnsureOwnerAsync(request.PlanId, cancellationToken);

        var installment = await _db.Installments
            .Include(i => i.CustomShares)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == request.InstallmentId && i.PlanId == request.PlanId, cancellationToken)
            ?? throw new NotFoundException(nameof(Installment), request.InstallmentId);

        installment.Name = request.Name.Trim();
        installment.DueDate = request.DueDate;
        installment.TotalAmount = request.TotalAmount;
        installment.ShareType = request.ShareType;
        installment.SortOrder = request.SortOrder;
        installment.UpdatedAtUtc = DateTime.UtcNow;

        // Flush share deletes first. Same-save Clear()+re-add can make EF issue UPDATEs
        // against already-removed share rows → DbUpdateConcurrencyException.
        var existingShares = installment.CustomShares.ToList();
        if (existingShares.Count > 0)
        {
            _db.InstallmentShares.RemoveRange(existingShares);
            installment.CustomShares.Clear();
            await _db.SaveChangesAsync(cancellationToken);
        }

        CreateInstallmentCommandHandler.ApplyCustomShares(
            installment,
            request.ShareType == ShareType.Custom ? request.CustomShares : null);
        CreateInstallmentCommandHandler.ValidateCustom(installment);

        await _db.SaveChangesAsync(cancellationToken);

        return ListInstallmentsQueryHandler.Map(installment);
    }
}

public sealed record DeleteInstallmentCommand(Guid PlanId, Guid InstallmentId) : IRequest;

public sealed class DeleteInstallmentCommandHandler : IRequestHandler<DeleteInstallmentCommand>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;

    public DeleteInstallmentCommandHandler(IAppDbContext db, IPlanAuthorization auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task Handle(DeleteInstallmentCommand request, CancellationToken cancellationToken)
    {
        await _auth.EnsureOwnerAsync(request.PlanId, cancellationToken);

        var installment = await _db.Installments
            .Include(i => i.CustomShares)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == request.InstallmentId && i.PlanId == request.PlanId, cancellationToken)
            ?? throw new NotFoundException(nameof(Installment), request.InstallmentId);

        var plan = await _db.Plans.FirstAsync(p => p.Id == request.PlanId, cancellationToken);
        if (plan.DeliveryInstallmentId == installment.Id)
        {
            plan.DeliveryInstallmentId = null;
        }

        _db.Payments.RemoveRange(installment.Payments);
        _db.InstallmentShares.RemoveRange(installment.CustomShares);
        _db.Installments.Remove(installment);
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public sealed record UpsertPaymentCommand(
    Guid PlanId,
    Guid InstallmentId,
    Guid PartnerId,
    bool IsPaid,
    DateOnly? PaidAt,
    Guid? PaidByPartnerId,
    string? Note) : IRequest<PaymentDto>;

public sealed class UpsertPaymentCommandValidator : AbstractValidator<UpsertPaymentCommand>
{
    public UpsertPaymentCommandValidator()
    {
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}

public sealed class UpsertPaymentCommandHandler : IRequestHandler<UpsertPaymentCommand, PaymentDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;
    private readonly IReceiptStorage _storage;
    private readonly ICurrentUser _currentUser;

    public UpsertPaymentCommandHandler(
        IAppDbContext db,
        IPlanAuthorization auth,
        IReceiptStorage storage,
        ICurrentUser currentUser)
    {
        _db = db;
        _auth = auth;
        _storage = storage;
        _currentUser = currentUser;
    }

    public async Task<PaymentDto> Handle(UpsertPaymentCommand request, CancellationToken cancellationToken)
    {
        await _auth.EnsureCanMarkPaymentAsync(request.PlanId, request.PartnerId, cancellationToken);
        var isOwner = await _auth.IsOwnerAsync(request.PlanId, cancellationToken);

        var installment = await _db.Installments.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == request.InstallmentId && i.PlanId == request.PlanId, cancellationToken)
            ?? throw new NotFoundException(nameof(Installment), request.InstallmentId);

        var partner = await _db.Partners.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PartnerId && p.PlanId == request.PlanId, cancellationToken)
            ?? throw new NotFoundException(nameof(Partner), request.PartnerId);

        if (request.IsPaid && InstallmentPaymentRules.IsFutureDueMonth(installment.DueDate))
        {
            throw new ValidationException(
                "İleri aylara ait taksitler için ödeme işaretlenemez. Sadece içinde bulunulan ay ve öncesi.");
        }

        if (request.PaidByPartnerId is Guid paidBy)
        {
            var payerOk = await _db.Partners.AnyAsync(
                p => p.Id == paidBy && p.PlanId == request.PlanId,
                cancellationToken);
            if (!payerOk)
            {
                throw new ValidationException("PaidByPartnerId must belong to this plan.");
            }
        }

        var plan = await _db.Plans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PlanId && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Plan), request.PlanId);

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
        }

        if (request.IsPaid && plan.RequireReceipt && string.IsNullOrEmpty(payment.ReceiptStorageKey))
        {
            throw new ValidationException("Dekont zorunlu. Önce dekont yükleyin.");
        }

        if (!request.IsPaid)
        {
            if (!string.IsNullOrEmpty(payment.ReceiptStorageKey))
            {
                await _storage.DeleteAsync(payment.ReceiptStorageKey, cancellationToken);
                payment.ReceiptStorageKey = null;
                payment.ReceiptContentType = null;
                payment.ReceiptFileName = null;
                payment.ReceiptUploadedAtUtc = null;
            }

            payment.IsPaid = false;
            payment.PaidAt = null;
            payment.PaidByPartnerId = null;
            payment.ReviewStatus = PaymentReviewStatus.None;
            payment.ReviewedAtUtc = null;
            payment.ReviewedByUserId = null;
            payment.Note = request.Note?.Trim() ?? string.Empty;
            payment.UpdatedAtUtc = DateTime.UtcNow;
            PlanActivity.Write(
                _db,
                _currentUser,
                request.PlanId,
                PlanActivityType.PaymentUpserted,
                $"{partner.Name} — {installment.Name}: ödeme geri alındı");
            await _db.SaveChangesAsync(cancellationToken);
            return payment.ToDto();
        }

        // Marking as paid
        if (!isOwner)
        {
            payment.IsPaid = false;
            payment.PaidAt = request.PaidAt ?? DateOnly.FromDateTime(DateTime.UtcNow);
            payment.PaidByPartnerId = request.PaidByPartnerId ?? request.PartnerId;
            payment.ReviewStatus = PaymentReviewStatus.Pending;
            payment.ReviewedAtUtc = null;
            payment.ReviewedByUserId = null;
            payment.Note = request.Note?.Trim() ?? string.Empty;
            payment.UpdatedAtUtc = DateTime.UtcNow;
            PlanActivity.Write(
                _db,
                _currentUser,
                request.PlanId,
                PlanActivityType.PaymentUpserted,
                $"{partner.Name} — {installment.Name}: onay için gönderildi");
        }
        else
        {
            payment.IsPaid = true;
            payment.PaidAt = request.PaidAt ?? DateOnly.FromDateTime(DateTime.UtcNow);
            payment.PaidByPartnerId = request.PaidByPartnerId ?? request.PartnerId;
            payment.ReviewStatus = PaymentReviewStatus.Approved;
            payment.ReviewedAtUtc = DateTime.UtcNow;
            payment.ReviewedByUserId = _currentUser.UserId;
            payment.Note = request.Note?.Trim() ?? string.Empty;
            payment.UpdatedAtUtc = DateTime.UtcNow;
            PlanActivity.Write(
                _db,
                _currentUser,
                request.PlanId,
                PlanActivityType.PaymentUpserted,
                $"{partner.Name} — {installment.Name}: ödendi işaretlendi");
        }

        await _db.SaveChangesAsync(cancellationToken);
        return payment.ToDto();
    }
}

public sealed record ApprovePaymentCommand(
    Guid PlanId,
    Guid InstallmentId,
    Guid PartnerId) : IRequest<PaymentDto>;

public sealed class ApprovePaymentCommandHandler : IRequestHandler<ApprovePaymentCommand, PaymentDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;
    private readonly ICurrentUser _currentUser;

    public ApprovePaymentCommandHandler(IAppDbContext db, IPlanAuthorization auth, ICurrentUser currentUser)
    {
        _db = db;
        _auth = auth;
        _currentUser = currentUser;
    }

    public async Task<PaymentDto> Handle(ApprovePaymentCommand request, CancellationToken cancellationToken)
    {
        await _auth.EnsureOwnerAsync(request.PlanId, cancellationToken);

        var payment = await _db.Payments
            .Include(p => p.Installment)
            .Include(p => p.Partner)
            .FirstOrDefaultAsync(
                p => p.InstallmentId == request.InstallmentId
                     && p.PartnerId == request.PartnerId
                     && p.Installment.PlanId == request.PlanId,
                cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), request.PartnerId);

        if (payment.ReviewStatus != PaymentReviewStatus.Pending)
        {
            throw new ValidationException("Bu ödeme onay bekleyen durumda değil.");
        }

        if (InstallmentPaymentRules.IsFutureDueMonth(payment.Installment.DueDate))
        {
            throw new ValidationException(
                "İleri aylara ait taksitler için ödeme onaylanamaz. Sadece içinde bulunulan ay ve öncesi.");
        }

        payment.IsPaid = true;
        payment.PaidAt ??= DateOnly.FromDateTime(DateTime.UtcNow);
        payment.PaidByPartnerId ??= payment.PartnerId;
        payment.ReviewStatus = PaymentReviewStatus.Approved;
        payment.ReviewedAtUtc = DateTime.UtcNow;
        payment.ReviewedByUserId = _currentUser.UserId;
        payment.UpdatedAtUtc = DateTime.UtcNow;

        PlanActivity.Write(
            _db,
            _currentUser,
            request.PlanId,
            PlanActivityType.PaymentApproved,
            $"{payment.Partner.Name} — {payment.Installment.Name}: ödeme onaylandı");

        await _db.SaveChangesAsync(cancellationToken);
        return payment.ToDto();
    }
}

public sealed record RejectPaymentCommand(
    Guid PlanId,
    Guid InstallmentId,
    Guid PartnerId,
    string? Note) : IRequest<PaymentDto>;

public sealed class RejectPaymentCommandHandler : IRequestHandler<RejectPaymentCommand, PaymentDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;
    private readonly ICurrentUser _currentUser;

    public RejectPaymentCommandHandler(IAppDbContext db, IPlanAuthorization auth, ICurrentUser currentUser)
    {
        _db = db;
        _auth = auth;
        _currentUser = currentUser;
    }

    public async Task<PaymentDto> Handle(RejectPaymentCommand request, CancellationToken cancellationToken)
    {
        await _auth.EnsureOwnerAsync(request.PlanId, cancellationToken);

        var payment = await _db.Payments
            .Include(p => p.Installment)
            .Include(p => p.Partner)
            .FirstOrDefaultAsync(
                p => p.InstallmentId == request.InstallmentId
                     && p.PartnerId == request.PartnerId
                     && p.Installment.PlanId == request.PlanId,
                cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), request.PartnerId);

        if (payment.ReviewStatus != PaymentReviewStatus.Pending)
        {
            throw new ValidationException("Bu ödeme onay bekleyen durumda değil.");
        }

        payment.IsPaid = false;
        payment.ReviewStatus = PaymentReviewStatus.Rejected;
        payment.ReviewedAtUtc = DateTime.UtcNow;
        payment.ReviewedByUserId = _currentUser.UserId;
        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            payment.Note = request.Note.Trim();
        }

        payment.UpdatedAtUtc = DateTime.UtcNow;

        PlanActivity.Write(
            _db,
            _currentUser,
            request.PlanId,
            PlanActivityType.PaymentRejected,
            $"{payment.Partner.Name} — {payment.Installment.Name}: ödeme reddedildi");

        await _db.SaveChangesAsync(cancellationToken);
        return payment.ToDto();
    }
}

public sealed record BulkIncreaseCommand(
    Guid PlanId,
    Guid FromInstallmentId,
    BulkIncreaseType Type,
    decimal Value) : IRequest<IReadOnlyList<InstallmentDto>>;

public sealed class BulkIncreaseCommandValidator : AbstractValidator<BulkIncreaseCommand>
{
    public BulkIncreaseCommandValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Value).GreaterThan(0m);
    }
}

public sealed class BulkIncreaseCommandHandler : IRequestHandler<BulkIncreaseCommand, IReadOnlyList<InstallmentDto>>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;

    public BulkIncreaseCommandHandler(IAppDbContext db, IPlanAuthorization auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task<IReadOnlyList<InstallmentDto>> Handle(BulkIncreaseCommand request, CancellationToken cancellationToken)
    {
        await _auth.EnsureOwnerAsync(request.PlanId, cancellationToken);

        var from = await _db.Installments
            .FirstOrDefaultAsync(i => i.Id == request.FromInstallmentId && i.PlanId == request.PlanId, cancellationToken)
            ?? throw new NotFoundException(nameof(Installment), request.FromInstallmentId);

        var targets = await _db.Installments
            .Include(i => i.CustomShares)
            .Include(i => i.Payments)
            .Where(i => i.PlanId == request.PlanId && i.SortOrder >= from.SortOrder)
            .OrderBy(i => i.SortOrder)
            .ToListAsync(cancellationToken);

        foreach (var inst in targets)
        {
            if (request.Type == BulkIncreaseType.Percent)
            {
                inst.TotalAmount = Math.Round(inst.TotalAmount * (1 + request.Value / 100m), 2);
            }
            else
            {
                inst.TotalAmount = Math.Round(inst.TotalAmount + request.Value, 2);
            }

            // Scale custom shares proportionally if present
            if (inst.ShareType == ShareType.Custom && inst.CustomShares.Count > 0)
            {
                var oldSum = inst.CustomShares.Sum(s => s.Amount);
                if (oldSum > 0)
                {
                    foreach (var share in inst.CustomShares)
                    {
                        share.Amount = Math.Round(share.Amount / oldSum * inst.TotalAmount, 2);
                    }

                    // Fix rounding drift on last share
                    var drift = inst.TotalAmount - inst.CustomShares.Sum(s => s.Amount);
                    if (drift != 0)
                    {
                        inst.CustomShares.Last().Amount += drift;
                    }
                }
            }

            inst.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return targets.Select(ListInstallmentsQueryHandler.Map).ToList();
    }
}
