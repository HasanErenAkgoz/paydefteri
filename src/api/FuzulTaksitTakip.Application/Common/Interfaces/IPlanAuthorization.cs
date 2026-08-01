using FuzulTaksitTakip.Domain.Entities;

namespace FuzulTaksitTakip.Application.Common.Interfaces;

public interface IPlanAuthorization
{
    Task EnsureOwnerAsync(Guid planId, CancellationToken cancellationToken = default);
    Task EnsureOwnerAsync(Plan plan, CancellationToken cancellationToken = default);
    Task EnsureMemberAsync(Guid planId, CancellationToken cancellationToken = default);
    Task EnsureCanMarkPaymentAsync(Guid planId, Guid partnerId, CancellationToken cancellationToken = default);
    Task<bool> IsOwnerAsync(Guid planId, CancellationToken cancellationToken = default);
    Task<Guid?> GetMyPartnerIdAsync(Guid planId, CancellationToken cancellationToken = default);
}
