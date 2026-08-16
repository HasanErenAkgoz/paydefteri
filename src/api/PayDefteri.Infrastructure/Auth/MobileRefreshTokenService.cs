using System.Security.Cryptography;
using System.Text;
using PayDefteri.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace PayDefteri.Infrastructure.Auth;

public sealed class MobileSessionOptions
{
    public const string SectionName = "MobileSession";
    public int RefreshTokenDays { get; set; } = 30;
}

public sealed class MobileRefreshTokenService : IMobileRefreshTokenService
{
    private readonly MobileSessionOptions _options;

    public MobileRefreshTokenService(IOptions<MobileSessionOptions> options)
    {
        _options = options.Value;
    }

    public string CreateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');

    public string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public DateTime GetExpiryUtc() => DateTime.UtcNow.AddDays(Math.Clamp(_options.RefreshTokenDays, 1, 90));
}
