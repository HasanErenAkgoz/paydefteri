namespace PayDefteri.Infrastructure.Identity;

public static class AppRoles
{
    public const string SuperAdmin = "SuperAdmin";
}

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public SuperAdminSeedOptions SuperAdmin { get; set; } = new();
}

public sealed class SuperAdminSeedOptions
{
    public bool Enabled { get; set; } = true;
    public string Email { get; set; } = "superadmin@paydefteri.com";
    public string Password { get; set; } = "";
    public string DisplayName { get; set; } = "Super Admin";
}
