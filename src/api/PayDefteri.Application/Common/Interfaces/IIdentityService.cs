namespace PayDefteri.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<(bool Succeeded, string? UserId, IEnumerable<string> Errors)> RegisterAsync(
        string email,
        string password,
        string displayName,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? UserId, string? Email, string? DisplayName, bool IsSuperAdmin)> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<(string? UserId, string? Email, string? DisplayName)> FindByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<(string? UserId, string? Email, string? DisplayName)> FindByIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<(string? UserId, string? Email, string? DisplayName, bool IsSuperAdmin)> FindSessionUserByIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, IEnumerable<string> Errors)> UpdateDisplayNameAsync(
        string userId,
        string displayName,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, IEnumerable<string> Errors)> ChangePasswordAsync(
        string userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);
}
