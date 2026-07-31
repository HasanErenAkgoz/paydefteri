namespace FuzulTaksitTakip.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<(bool Succeeded, string? UserId, IEnumerable<string> Errors)> RegisterAsync(
        string email,
        string password,
        string displayName,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? UserId, string? Email, string? DisplayName)> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);
}
