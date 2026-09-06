using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PayDefteri.Application.Common.Exceptions;
using PayDefteri.Application.Common.Interfaces;

namespace PayDefteri.Application.Auth;

/// <summary>
/// Permanently deletes the signed-in account and the data it owns. Required by
/// the Play Store account deletion policy, so the flow has to be reachable from
/// inside the app and must leave nothing behind that identifies the user.
/// </summary>
public sealed record DeleteAccountCommand(string CurrentPassword) : IRequest;

public sealed class DeleteAccountCommandValidator : AbstractValidator<DeleteAccountCommand>
{
    public DeleteAccountCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Hesabı silmek için şifrenizi girin.");
    }
}

public sealed class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand>
{
    /// <summary>Partner name left behind in other owners' plans, whose history stays intact.</summary>
    private const string RemovedPartnerName = "Silinmiş kullanıcı";

    private readonly ICurrentUser _currentUser;
    private readonly IIdentityService _identity;
    private readonly IAppDbContext _db;
    private readonly IReceiptStorage _receipts;

    public DeleteAccountCommandHandler(
        ICurrentUser currentUser,
        IIdentityService identity,
        IAppDbContext db,
        IReceiptStorage receipts)
    {
        _currentUser = currentUser;
        _identity = identity;
        _db = db;
        _receipts = receipts;
    }

    public async Task Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();

        if (!await _identity.CheckPasswordAsync(userId, request.CurrentPassword, cancellationToken))
        {
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure(
                    nameof(request.CurrentPassword),
                    "Mevcut şifre hatalı."),
            });
        }

        var user = await _identity.FindByIdAsync(userId, cancellationToken);
        if (user.UserId is null || user.Email is null)
        {
            throw new NotFoundException("User", userId);
        }

        var ownedPlanIds = await _db.Plans
            .Where(p => p.OwnerUserId == userId)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        // Receipt files live outside the database, so collect their keys before
        // the cascade removes the rows that point at them.
        var receiptKeys = await _db.Payments
            .Where(p => ownedPlanIds.Contains(p.Installment.PlanId) && p.ReceiptStorageKey != null)
            .Select(p => p.ReceiptStorageKey!)
            .ToListAsync(cancellationToken);

        await _db.ExecuteInTransactionAsync(async ct =>
        {
            var ownedPlans = await _db.Plans
                .Where(p => ownedPlanIds.Contains(p.Id))
                .ToListAsync(ct);
            _db.Plans.RemoveRange(ownedPlans);

            // Plans owned by someone else keep their history; only the link back
            // to this account and the membership go away.
            var linkedPartners = await _db.Partners
                .Where(p => p.LinkedUserId == userId && !ownedPlanIds.Contains(p.PlanId))
                .ToListAsync(ct);
            foreach (var partner in linkedPartners)
            {
                partner.LinkedUserId = null;
                partner.InviteEmail = null;
                partner.Name = RemovedPartnerName;
            }

            var memberships = await _db.PlanMembers
                .Where(m => m.UserId == userId)
                .ToListAsync(ct);
            _db.PlanMembers.RemoveRange(memberships);

            var invites = await _db.PlanInvites
                .Where(i => i.InvitedByUserId == userId || i.Email == user.Email)
                .ToListAsync(ct);
            _db.PlanInvites.RemoveRange(invites);

            var statements = await _db.SpendingStatements
                .Where(s => s.OwnerUserId == userId)
                .ToListAsync(ct);
            _db.SpendingStatements.RemoveRange(statements);

            var merchantPreferences = await _db.SpendingMerchantPreferences
                .Where(p => p.OwnerUserId == userId)
                .ToListAsync(ct);
            _db.SpendingMerchantPreferences.RemoveRange(merchantPreferences);

            var budgets = await _db.SpendingCategoryBudgets
                .Where(b => b.OwnerUserId == userId)
                .ToListAsync(ct);
            _db.SpendingCategoryBudgets.RemoveRange(budgets);

            var sessions = await _db.MobileRefreshSessions
                .Where(s => s.UserId == userId)
                .ToListAsync(ct);
            _db.MobileRefreshSessions.RemoveRange(sessions);

            await _db.SaveChangesAsync(ct);
            return true;
        }, cancellationToken);

        var (succeeded, errors) = await _identity.DeleteUserAsync(userId, cancellationToken);
        if (!succeeded)
        {
            throw new ValidationException(
                errors.Select(e => new FluentValidation.Results.ValidationFailure(string.Empty, e)));
        }

        // Best effort: the account is already gone, so a storage hiccup must not
        // turn a completed deletion into an error for the caller.
        foreach (var key in receiptKeys)
        {
            try
            {
                await _receipts.DeleteAsync(key, cancellationToken);
            }
            catch (IOException)
            {
            }
        }
    }
}
