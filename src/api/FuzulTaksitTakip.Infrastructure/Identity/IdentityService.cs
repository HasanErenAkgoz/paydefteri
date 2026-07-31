using FuzulTaksitTakip.Application.Common.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace FuzulTaksitTakip.Infrastructure.Identity;

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
            return (false, null, result.Errors.Select(e => e.Description));
        }

        return (true, user.Id, Array.Empty<string>());
    }

    public async Task<(bool Succeeded, string? UserId, string? Email, string? DisplayName)> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return (false, null, null, null);
        }

        var ok = await _userManager.CheckPasswordAsync(user, password);
        if (!ok)
        {
            return (false, null, null, null);
        }

        return (true, user.Id, user.Email, user.DisplayName);
    }
}
