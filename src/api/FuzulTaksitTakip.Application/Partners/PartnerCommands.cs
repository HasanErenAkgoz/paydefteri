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

namespace FuzulTaksitTakip.Application.Partners;

public sealed record ListPartnersQuery(Guid PlanId) : IRequest<IReadOnlyList<PartnerDto>>;

public sealed class ListPartnersQueryHandler : IRequestHandler<ListPartnersQuery, IReadOnlyList<PartnerDto>>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;

    public ListPartnersQueryHandler(IAppDbContext db, IPlanAuthorization auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task<IReadOnlyList<PartnerDto>> Handle(ListPartnersQuery request, CancellationToken cancellationToken)
    {
        await _auth.EnsureMemberAsync(request.PlanId, cancellationToken);

        var list = await _db.Partners.AsNoTracking()
            .Where(p => p.PlanId == request.PlanId)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(cancellationToken);

        return list.Select(p => p.ToDto()).ToList();
    }
}

public sealed record CreatePartnerCommand(
    Guid PlanId,
    string Name,
    string Color,
    decimal DefaultPct,
    int SortOrder,
    string? Iban) : IRequest<PartnerDto>;

public sealed class CreatePartnerCommandValidator : AbstractValidator<CreatePartnerCommand>
{
    public CreatePartnerCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(32);
        RuleFor(x => x.DefaultPct).InclusiveBetween(0m, 100m);
        RuleFor(x => x.Iban)
            .Must(iban => iban is null || string.IsNullOrWhiteSpace(iban) || IbanNormalizer.IsValidTurkishIban(iban))
            .WithMessage("Geçerli bir TR IBAN girin.");
    }
}

public sealed class CreatePartnerCommandHandler : IRequestHandler<CreatePartnerCommand, PartnerDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;
    private readonly ICurrentUser _currentUser;

    public CreatePartnerCommandHandler(IAppDbContext db, IPlanAuthorization auth, ICurrentUser currentUser)
    {
        _db = db;
        _auth = auth;
        _currentUser = currentUser;
    }

    public async Task<PartnerDto> Handle(CreatePartnerCommand request, CancellationToken cancellationToken)
    {
        await _auth.EnsureOwnerAsync(request.PlanId, cancellationToken);

        var partner = new Partner
        {
            PlanId = request.PlanId,
            Name = request.Name.Trim(),
            Color = request.Color,
            DefaultPct = request.DefaultPct,
            SortOrder = request.SortOrder,
            Iban = IbanNormalizer.Normalize(request.Iban)
        };

        _db.Partners.Add(partner);
        PlanActivity.Write(_db, _currentUser, request.PlanId, PlanActivityType.PartnerCreated, $"Ortak eklendi: {partner.Name}");
        await _db.SaveChangesAsync(cancellationToken);

        return partner.ToDto();
    }
}

public sealed record UpdatePartnerCommand(
    Guid PlanId,
    Guid PartnerId,
    string Name,
    string Color,
    decimal DefaultPct,
    int SortOrder,
    string? Iban) : IRequest<PartnerDto>;

public sealed class UpdatePartnerCommandValidator : AbstractValidator<UpdatePartnerCommand>
{
    public UpdatePartnerCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(32);
        RuleFor(x => x.DefaultPct).InclusiveBetween(0m, 100m);
        RuleFor(x => x.Iban)
            .Must(iban => iban is null || string.IsNullOrWhiteSpace(iban) || IbanNormalizer.IsValidTurkishIban(iban))
            .WithMessage("Geçerli bir TR IBAN girin.");
    }
}

public sealed class UpdatePartnerCommandHandler : IRequestHandler<UpdatePartnerCommand, PartnerDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;

    public UpdatePartnerCommandHandler(IAppDbContext db, IPlanAuthorization auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task<PartnerDto> Handle(UpdatePartnerCommand request, CancellationToken cancellationToken)
    {
        await _auth.EnsureOwnerAsync(request.PlanId, cancellationToken);

        var partner = await _db.Partners
            .FirstOrDefaultAsync(p => p.Id == request.PartnerId && p.PlanId == request.PlanId, cancellationToken)
            ?? throw new NotFoundException(nameof(Partner), request.PartnerId);

        partner.Name = request.Name.Trim();
        partner.Color = request.Color;
        partner.DefaultPct = request.DefaultPct;
        partner.SortOrder = request.SortOrder;
        partner.Iban = IbanNormalizer.Normalize(request.Iban);
        partner.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return partner.ToDto();
    }
}

public sealed record DeletePartnerCommand(Guid PlanId, Guid PartnerId) : IRequest;

public sealed class DeletePartnerCommandHandler : IRequestHandler<DeletePartnerCommand>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;

    public DeletePartnerCommandHandler(IAppDbContext db, IPlanAuthorization auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task Handle(DeletePartnerCommand request, CancellationToken cancellationToken)
    {
        await _auth.EnsureOwnerAsync(request.PlanId, cancellationToken);

        var partner = await _db.Partners
            .FirstOrDefaultAsync(p => p.Id == request.PartnerId && p.PlanId == request.PlanId, cancellationToken)
            ?? throw new NotFoundException(nameof(Partner), request.PartnerId);

        var hasPayments = await _db.Payments.AnyAsync(
            p => p.PartnerId == request.PartnerId || p.PaidByPartnerId == request.PartnerId,
            cancellationToken);

        if (hasPayments)
        {
            throw new ConflictException("Cannot delete partner with existing payments. Clear payments first.");
        }

        var shares = await _db.InstallmentShares
            .Where(s => s.PartnerId == request.PartnerId)
            .ToListAsync(cancellationToken);
        _db.InstallmentShares.RemoveRange(shares);

        _db.Partners.Remove(partner);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
