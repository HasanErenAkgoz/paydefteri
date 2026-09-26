namespace PayDefteri.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<(bool Succeeded, string? UserId, IEnumerable<string> Errors)> RegisterAsync(
        string email,
        string password,
        string displayName,
        bool emailConfirmed,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? UserId, string? Email, string? DisplayName, bool IsSuperAdmin)> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<(string? UserId, string? Email, string? DisplayName)> FindByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the account behind an external sign-in (today: Google). Matches
    /// the stored login first, then falls back to the address so an existing
    /// password account is linked instead of duplicated, and otherwise creates a
    /// passwordless account.
    /// </summary>
    Task<(bool Succeeded, string? UserId, string? Email, string? DisplayName, bool IsSuperAdmin, bool Created, IEnumerable<string> Errors)> FindOrCreateExternalUserAsync(
        string provider,
        string providerKey,
        string email,
        string displayName,
        CancellationToken cancellationToken = default);

    /// <summary>False for an account that only ever signed in through a provider.</summary>
    Task<bool> HasPasswordAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<(string? UserId, string? Email, string? DisplayName)> FindByIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<(string? UserId, string? Email, string? DisplayName, bool IsSuperAdmin)> FindSessionUserByIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<(string? Token, string? Email, string? DisplayName)> CreateEmailConfirmationTokenAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, IEnumerable<string> Errors)> ConfirmEmailAsync(
        string userId,
        string token,
        CancellationToken cancellationToken = default);

    Task<bool> IsEmailConfirmedAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<bool> CheckPasswordAsync(
        string userId,
        string password,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, IEnumerable<string> Errors)> DeleteUserAsync(
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
