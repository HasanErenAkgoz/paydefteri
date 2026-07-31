using FluentValidation;
using FuzulTaksitTakip.Application.Common.Exceptions;
using FuzulTaksitTakip.Application.Common.Interfaces;
using FuzulTaksitTakip.Application.Common.Models;
using FuzulTaksitTakip.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FuzulTaksitTakip.Application.Plans;

public sealed record GetPlanQuery(Guid PlanId) : IRequest<PlanDto>;

public sealed class GetPlanQueryHandler : IRequestHandler<GetPlanQuery, PlanDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;

    public GetPlanQueryHandler(IAppDbContext db, IPlanAuthorization auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task<PlanDto> Handle(GetPlanQuery request, CancellationToken cancellationToken)
    {
        await _auth.EnsureOwnerAsync(request.PlanId, cancellationToken);

        var plan = await _db.Plans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken)
            ?? throw new NotFoundException(nameof(Plan), request.PlanId);

        return new PlanDto(plan.Id, plan.Title, plan.Description, plan.DeliveryInstallmentId, plan.CreatedAtUtc);
    }
}

public sealed record CreatePlanCommand(string Title, string Description) : IRequest<PlanDto>;

public sealed class CreatePlanCommandValidator : AbstractValidator<CreatePlanCommand>
{
    public CreatePlanCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public sealed class CreatePlanCommandHandler : IRequestHandler<CreatePlanCommand, PlanDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CreatePlanCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PlanDto> Handle(CreatePlanCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var plan = new Plan
        {
            OwnerUserId = userId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? string.Empty
        };

        _db.Plans.Add(plan);
        await _db.SaveChangesAsync(cancellationToken);

        return new PlanDto(plan.Id, plan.Title, plan.Description, plan.DeliveryInstallmentId, plan.CreatedAtUtc);
    }
}

public sealed record UpdatePlanCommand(
    Guid PlanId,
    string Title,
    string Description,
    Guid? DeliveryInstallmentId) : IRequest<PlanDto>;

public sealed class UpdatePlanCommandValidator : AbstractValidator<UpdatePlanCommand>
{
    public UpdatePlanCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public sealed class UpdatePlanCommandHandler : IRequestHandler<UpdatePlanCommand, PlanDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;

    public UpdatePlanCommandHandler(IAppDbContext db, IPlanAuthorization auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task<PlanDto> Handle(UpdatePlanCommand request, CancellationToken cancellationToken)
    {
        await _auth.EnsureOwnerAsync(request.PlanId, cancellationToken);

        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken)
            ?? throw new NotFoundException(nameof(Plan), request.PlanId);

        if (request.DeliveryInstallmentId is Guid deliveryId)
        {
            var exists = await _db.Installments.AnyAsync(
                i => i.Id == deliveryId && i.PlanId == request.PlanId,
                cancellationToken);
            if (!exists)
            {
                throw new ValidationException("DeliveryInstallmentId must belong to this plan.");
            }
        }

        plan.Title = request.Title.Trim();
        plan.Description = request.Description?.Trim() ?? string.Empty;
        plan.DeliveryInstallmentId = request.DeliveryInstallmentId;
        plan.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return new PlanDto(plan.Id, plan.Title, plan.Description, plan.DeliveryInstallmentId, plan.CreatedAtUtc);
    }
}

public sealed record DeletePlanCommand(Guid PlanId) : IRequest;

public sealed class DeletePlanCommandHandler : IRequestHandler<DeletePlanCommand>
{
    private readonly IAppDbContext _db;
    private readonly IPlanAuthorization _auth;

    public DeletePlanCommandHandler(IAppDbContext db, IPlanAuthorization auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task Handle(DeletePlanCommand request, CancellationToken cancellationToken)
    {
        await _auth.EnsureOwnerAsync(request.PlanId, cancellationToken);

        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken)
            ?? throw new NotFoundException(nameof(Plan), request.PlanId);

        _db.Plans.Remove(plan);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
