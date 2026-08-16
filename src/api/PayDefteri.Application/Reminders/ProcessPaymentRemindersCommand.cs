using PayDefteri.Application.Common.Interfaces;
using PayDefteri.Domain.Entities;
using PayDefteri.Domain.Enums;
using PayDefteri.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace PayDefteri.Application.Reminders;

public sealed record ProcessPaymentRemindersCommand(DateOnly? TodayOverride = null) : IRequest<ProcessPaymentRemindersResult>;

public sealed record ProcessPaymentRemindersResult(int PartnerEmails, int OwnerEmails, int SkippedLogged);

public sealed class ProcessPaymentRemindersCommandHandler
    : IRequestHandler<ProcessPaymentRemindersCommand, ProcessPaymentRemindersResult>
{
    private static readonly TimeZoneInfo TurkeyTz = ResolveTurkeyTz();

    private readonly IAppDbContext _db;
    private readonly IIdentityService _identity;
    private readonly IReminderEmailService _emails;
    private readonly ILogger<ProcessPaymentRemindersCommandHandler> _logger;

    public ProcessPaymentRemindersCommandHandler(
        IAppDbContext db,
        IIdentityService identity,
        IReminderEmailService emails,
        ILogger<ProcessPaymentRemindersCommandHandler> logger)
    {
        _db = db;
        _identity = identity;
        _emails = emails;
        _logger = logger;
    }

    public async Task<ProcessPaymentRemindersResult> Handle(
        ProcessPaymentRemindersCommand request,
        CancellationToken cancellationToken)
    {
        var today = request.TodayOverride ?? TodayInTurkey();
        var partnerEmails = 0;
        var ownerEmails = 0;
        var skipped = 0;

        var plans = await _db.Plans
            .AsNoTracking()
            .Where(p => p.RemindersEnabled && !p.IsDeleted)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.OwnerUserId,
                Before = p.ReminderDaysBefore,
                After = p.ReminderDaysAfter
            })
            .ToListAsync(cancellationToken);

        foreach (var planInfo in plans)
        {
            var before = NormalizeOffsets(planInfo.Before);
            var after = NormalizeOffsets(planInfo.After);
            if (before.Count == 0 && after.Count == 0)
            {
                continue;
            }

            var plan = await _db.Plans
                .Include(p => p.Partners)
                .Include(p => p.Installments)
                    .ThenInclude(i => i.Payments)
                .Include(p => p.Installments)
                    .ThenInclude(i => i.CustomShares)
                .FirstAsync(p => p.Id == planInfo.Id, cancellationToken);

            var partners = plan.Partners.OrderBy(p => p.SortOrder).ToList();
            var owner = await _identity.FindByIdAsync(plan.OwnerUserId, cancellationToken);

            foreach (var installment in plan.Installments.OrderBy(i => i.SortOrder))
            {
                var daysUntilDue = installment.DueDate.DayNumber - today.DayNumber;
                PaymentReminderKind? kind = null;
                int offset = 0;

                if (daysUntilDue > 0 && before.Contains(daysUntilDue))
                {
                    kind = PaymentReminderKind.Before;
                    offset = daysUntilDue;
                }
                else if (daysUntilDue < 0 && after.Contains(-daysUntilDue))
                {
                    kind = PaymentReminderKind.After;
                    offset = -daysUntilDue;
                }
                else if (daysUntilDue == 0 && before.Contains(0))
                {
                    kind = PaymentReminderKind.Before;
                    offset = 0;
                }

                if (kind is null)
                {
                    continue;
                }

                var unpaid = partners
                    .Select(partner =>
                    {
                        var payment = installment.Payments.FirstOrDefault(p => p.PartnerId == partner.Id);
                        var isPaid = payment?.IsPaid == true;
                        return (Partner: partner, IsPaid: isPaid, Amount: ShareCalculator.GetPartnerShare(installment, partner, partners));
                    })
                    .Where(x => !x.IsPaid)
                    .ToList();

                if (unpaid.Count == 0)
                {
                    continue;
                }

                var isOverdue = kind == PaymentReminderKind.After;

                foreach (var row in unpaid)
                {
                    if (string.IsNullOrWhiteSpace(row.Partner.LinkedUserId))
                    {
                        continue;
                    }

                    if (await AlreadySentAsync(plan.Id, installment.Id, row.Partner.Id, kind.Value, offset, today, cancellationToken))
                    {
                        skipped++;
                        continue;
                    }

                    var user = await _identity.FindByIdAsync(row.Partner.LinkedUserId, cancellationToken);
                    if (user.Email is null)
                    {
                        continue;
                    }

                    try
                    {
                        await _emails.SendPartnerReminderAsync(
                            new PartnerPaymentReminderEmail(
                                user.Email,
                                user.DisplayName ?? row.Partner.Name,
                                plan.Title,
                                plan.Id,
                                installment.Name,
                                installment.DueDate,
                                row.Amount,
                                isOverdue,
                                offset),
                            cancellationToken);

                        await WriteLogAsync(plan.Id, installment.Id, row.Partner.Id, kind.Value, offset, today, cancellationToken);
                        partnerEmails++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Partner reminder failed plan={PlanId} installment={InstallmentId} partner={PartnerId}",
                            plan.Id, installment.Id, row.Partner.Id);
                    }
                }

                if (owner.Email is null)
                {
                    continue;
                }

                if (await AlreadySentAsync(plan.Id, installment.Id, null, kind.Value, offset, today, cancellationToken))
                {
                    skipped++;
                    continue;
                }

                try
                {
                    await _emails.SendOwnerReminderAsync(
                        new OwnerPaymentReminderEmail(
                            owner.Email,
                            plan.Title,
                            plan.Id,
                            installment.Name,
                            installment.DueDate,
                            isOverdue,
                            offset,
                            unpaid.Select(u => new OwnerReminderUnpaidRow(
                                u.Partner.Name,
                                u.Amount,
                                !string.IsNullOrWhiteSpace(u.Partner.LinkedUserId))).ToList()),
                        cancellationToken);

                    await WriteLogAsync(plan.Id, installment.Id, null, kind.Value, offset, today, cancellationToken);
                    ownerEmails++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Owner reminder failed plan={PlanId} installment={InstallmentId}",
                        plan.Id, installment.Id);
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new ProcessPaymentRemindersResult(partnerEmails, ownerEmails, skipped);
    }

    private async Task<bool> AlreadySentAsync(
        Guid planId,
        Guid installmentId,
        Guid? partnerId,
        PaymentReminderKind kind,
        int offsetDays,
        DateOnly sentOn,
        CancellationToken cancellationToken)
    {
        return await _db.PaymentReminderLogs.AnyAsync(
            l => l.PlanId == planId
                 && l.InstallmentId == installmentId
                 && l.PartnerId == partnerId
                 && l.Kind == kind
                 && l.OffsetDays == offsetDays
                 && l.SentOn == sentOn,
            cancellationToken);
    }

    private async Task WriteLogAsync(
        Guid planId,
        Guid installmentId,
        Guid? partnerId,
        PaymentReminderKind kind,
        int offsetDays,
        DateOnly sentOn,
        CancellationToken cancellationToken)
    {
        _db.PaymentReminderLogs.Add(new PaymentReminderLog
        {
            PlanId = planId,
            InstallmentId = installmentId,
            PartnerId = partnerId,
            Kind = kind,
            OffsetDays = offsetDays,
            SentOn = sentOn
        });
        await Task.CompletedTask;
    }

    private static HashSet<int> NormalizeOffsets(int[]? raw) =>
        (raw ?? Array.Empty<int>())
        .Where(d => d is >= 0 and <= 90)
        .Distinct()
        .ToHashSet();

    private static DateOnly TodayInTurkey()
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TurkeyTz);
        return DateOnly.FromDateTime(local);
    }

    private static TimeZoneInfo ResolveTurkeyTz()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        }
    }
}
