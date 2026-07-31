using FuzulTaksitTakip.Domain.Entities;

namespace FuzulTaksitTakip.Application.Common.Interfaces;

public interface IPlanAuthorization
{
    Task EnsureOwnerAsync(Guid planId, CancellationToken cancellationToken = default);
    Task EnsureOwnerAsync(Plan plan, CancellationToken cancellationToken = default);
}
