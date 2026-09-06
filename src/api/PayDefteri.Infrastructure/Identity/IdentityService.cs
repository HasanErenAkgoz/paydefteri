using PayDefteri.Application.Common.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace PayDefteri.Infrastructure.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<AppUser> _userManager;

    public IdentityService(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<(bool Succeeded, string? UserId, IEnumerable<string> Errors)> RegisterAsync(
        string email,
        string password,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            DisplayName = displayName,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return (false, null, result.Errors.Select(LocalizeIdentityError));
        }

        return (true, user.Id, Array.Empty<string>());
    }

    private static string LocalizeIdentityError(IdentityError error) =>
        error.Code switch
        {
            "DuplicateUserName" or "DuplicateEmail" =>
                "Bu e-posta zaten kayıtlı. Giriş yapıp devam edin.",
            "PasswordTooShort" => "Şifre en az 6 karakter olmalı.",
            "PasswordRequiresNonAlphanumeric" or "PasswordRequiresDigit"
                or "PasswordRequiresLower" or "PasswordRequiresUpper" =>
                "Şifre gereksinimleri karşılanmıyor.",
            _ => string.IsNullOrWhiteSpace(error.Description)
                ? "Kayıt tamamlanamadı."
                : error.Description,
        };

    public async Task<(bool Succeeded, string? UserId, string? Email, string? DisplayName, bool IsSuperAdmin)> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return (false, null, null, null, false);
        }

        var ok = await _userManager.CheckPasswordAsync(user, password);
        if (!ok)
        {
            return (false, null, null, null, false);
        }

        var isSuperAdmin = await _userManager.IsInRoleAsync(user, AppRoles.SuperAdmin);
        return (true, user.Id, user.Email, user.DisplayName, isSuperAdmin);
    }

    public async Task<(string? UserId, string? Email, string? DisplayName)> FindByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user is null ? (null, null, null) : (user.Id, user.Email, user.DisplayName);
    }

    public async Task<(string? UserId, string? Email, string? DisplayName)> FindByIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user is null ? (null, null, null) : (user.Id, user.Email, user.DisplayName);
    }

    public async Task<(string? UserId, string? Email, string? DisplayName, bool IsSuperAdmin)> FindSessionUserByIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return (null, null, null, false);
        }

        return (user.Id, user.Email, user.DisplayName,
            await _userManager.IsInRoleAsync(user, AppRoles.SuperAdmin));
    }

    public async Task<bool> CheckPasswordAsync(
        string userId,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user is not null && await _userManager.CheckPasswordAsync(user, password);
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> DeleteUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return (false, new[] { "Kullanıcı bulunamadı." });
        }

        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded
            ? (true, Array.Empty<string>())
            : (false, result.Errors.Select(e => e.Description));
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> UpdateDisplayNameAsync(
        string userId,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return (false, new[] { "Kullanıcı bulunamadı." });
        }

        user.DisplayName = displayName;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded
            ? (true, Array.Empty<string>())
            : (false, result.Errors.Select(e => e.Description));
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> ChangePasswordAsync(
        string userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return (false, new[] { "Kullanıcı bulunamadı." });
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (result.Succeeded)
        {
            return (true, Array.Empty<string>());
        }

        var errors = result.Errors.Select(e =>
            e.Code is "PasswordMismatch"
                ? "Mevcut şifre hatalı."
                : e.Description);
        return (false, errors);
    }
}
