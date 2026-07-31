using FluentValidation;
using FuzulTaksitTakip.Application.Common.Exceptions;
using FuzulTaksitTakip.Application.Common.Interfaces;
using FuzulTaksitTakip.Application.Common.Models;
using FuzulTaksitTakip.Domain.Entities;
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
        await _auth.EnsureOwnerAsync(request.PlanId, cancellationToken);

        return await _db.Partners.AsNoTracking()
            .Where(p => p.PlanId == request.PlanId)
            .OrderBy(p => p.SortOrder)
            .Select(p => new PartnerDto(p.Id, p.PlanId, p.Name, p.Color, p.DefaultPct, p.SortOrder))
            .ToListAsync(cancellationToken);
    }
}

public sealed record CreatePartnerCommand(
    Guid PlanId,
    string Name,
    string Color,
    decimal DefaultPct,
    int SortOrder) : IRequest<PartnerDto>;

public sealed class CreatePartnerCommandValidator : AbstractValidator<CreatePartnerCommand>
{
    public CreatePartnerCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(32);
        RuleFor(x => x.DefaultPct).InclusiveBetween(0m, 100m);
    }
}

public sealed class CreatePartnerCommandHandler : IRequestHandler<CreatePartnerCommand, PartnerDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;

    public CreatePartnerCommandHandler(IAppDbContext db, IPlanAuthorization auth)
    {
        _db = db;
        _auth = auth;
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
            SortOrder = request.SortOrder
        };

        _db.Partners.Add(partner);
        await _db.SaveChangesAsync(cancellationToken);

        return new PartnerDto(partner.Id, partner.PlanId, partner.Name, partner.Color, partner.DefaultPct, partner.SortOrder);
    }
}

public sealed record UpdatePartnerCommand(
    Guid PlanId,
    Guid PartnerId,
    string Name,
    string Color,
    decimal DefaultPct,
    int SortOrder) : IRequest<PartnerDto>;

public sealed class UpdatePartnerCommandValidator : AbstractValidator<UpdatePartnerCommand>
{
    public UpdatePartnerCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(32);
        RuleFor(x => x.DefaultPct).InclusiveBetween(0m, 100m);
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
        partner.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return new PartnerDto(partner.Id, partner.PlanId, partner.Name, partner.Color, partner.DefaultPct, partner.SortOrder);
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
