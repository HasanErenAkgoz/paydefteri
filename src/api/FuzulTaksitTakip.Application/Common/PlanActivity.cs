using FuzulTaksitTakip.Application.Common.Interfaces;
using FuzulTaksitTakip.Domain.Entities;
using FuzulTaksitTakip.Domain.Enums;

namespace FuzulTaksitTakip.Application.Common;

public static class PlanActivity
{
    public static void Write(
        IAppDbContext db,
        ICurrentUser user,
        Guid planId,
        PlanActivityType type,
        string message)
    {
        var userId = user.UserId ?? "unknown";
        db.PlanActivityLogs.Add(new PlanActivityLog
        {
            PlanId = planId,
            ActorUserId = userId,
            ActorDisplayName = string.IsNullOrWhiteSpace(user.DisplayName) ? userId : user.DisplayName!,
            Type = type,
            Message = message.Trim()
        });
    }
}
