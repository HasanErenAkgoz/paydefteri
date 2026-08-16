using Microsoft.AspNetCore.Identity;

namespace PayDefteri.Infrastructure.Identity;

public class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}
