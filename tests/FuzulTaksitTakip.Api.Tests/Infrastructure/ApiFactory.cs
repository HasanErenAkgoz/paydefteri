using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FuzulTaksitTakip.Api.Tests.Infrastructure;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            "Host=localhost;Port=5432;Database=taksitle_tests;Username=taksitle;Password=taksitle");
        builder.UseSetting("Jwt:Issuer", "PayDefteri");
        builder.UseSetting("Jwt:Audience", "PayDefteri");
        builder.UseSetting("Jwt:Key", "test-only-paydefteri-jwt-secret-key-9f3a7c2e1b8d4e6a0c5f7d2b9e1a3c8f");
        builder.UseSetting("Jwt:ExpiryMinutes", "60");
        builder.UseSetting("Email:Enabled", "false");
        builder.UseSetting("Reminders:RunOnStartup", "false");
        builder.UseSetting("App:PublicWebUrl", "http://localhost:4200");
    }
}
