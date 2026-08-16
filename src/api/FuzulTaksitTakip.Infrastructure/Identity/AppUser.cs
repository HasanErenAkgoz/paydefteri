using Microsoft.AspNetCore.Identity;

namespace FuzulTaksitTakip.Infrastructure.Identity;

public class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}
