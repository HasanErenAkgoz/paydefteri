using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FuzulTaksitTakip.Infrastructure.Identity;

public static class IdentityDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var options = services.GetRequiredService<IOptions<SeedOptions>>().Value.SuperAdmin;
        if (!options.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
        {
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("IdentityDataSeeder");
            logger.LogWarning("SuperAdmin seed skipped: Seed:SuperAdmin Email/Password missing.");
            return;
        }

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var log = services.GetRequiredService<ILoggerFactory>().CreateLogger("IdentityDataSeeder");

        if (!await roleManager.RoleExistsAsync(AppRoles.SuperAdmin))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(AppRoles.SuperAdmin));
            if (!roleResult.Succeeded)
            {
                log.LogError(
                    "Failed to create SuperAdmin role: {Errors}",
                    string.Join("; ", roleResult.Errors.Select(e => e.Description)));
                return;
            }
        }

        var email = options.Email.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new AppUser
            {
                UserName = email,
                Email = email,
                DisplayName = options.DisplayName.Trim(),
                EmailConfirmed = true
            };

            var create = await userManager.CreateAsync(user, options.Password);
            if (!create.Succeeded)
            {
                log.LogError(
                    "Failed to create SuperAdmin user: {Errors}",
                    string.Join("; ", create.Errors.Select(e => e.Description)));
                return;
            }

            log.LogInformation("SuperAdmin user created ({Email}).", email);
        }
        else
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var reset = await userManager.ResetPasswordAsync(user, token, options.Password);
            if (!reset.Succeeded)
            {
                log.LogError(
                    "Failed to reset SuperAdmin password: {Errors}",
                    string.Join("; ", reset.Errors.Select(e => e.Description)));
                return;
            }

            if (!string.Equals(user.DisplayName, options.DisplayName.Trim(), StringComparison.Ordinal))
            {
                user.DisplayName = options.DisplayName.Trim();
                await userManager.UpdateAsync(user);
            }

            log.LogInformation("SuperAdmin password synced ({Email}).", email);
        }

        if (!await userManager.IsInRoleAsync(user, AppRoles.SuperAdmin))
        {
            var addRole = await userManager.AddToRoleAsync(user, AppRoles.SuperAdmin);
            if (!addRole.Succeeded)
            {
                log.LogError(
                    "Failed to assign SuperAdmin role: {Errors}",
                    string.Join("; ", addRole.Errors.Select(e => e.Description)));
            }
        }
    }
}
