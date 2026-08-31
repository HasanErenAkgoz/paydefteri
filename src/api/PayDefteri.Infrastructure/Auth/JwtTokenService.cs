using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using PayDefteri.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace PayDefteri.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "PayDefteri";
    public string Audience { get; set; } = "PayDefteri";
    public string Key { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 30;
    public int RememberMeDays { get; set; } = 30;
}

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(Microsoft.Extensions.Options.IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public (string AccessToken, DateTime ExpiresAtUtc) CreateToken(
        string userId,
        string email,
        string displayName,
        bool isSuperAdmin = false,
        bool rememberMe = false)
    {
        var lifetime = rememberMe
            ? TimeSpan.FromDays(Math.Clamp(_options.RememberMeDays, 1, 90))
            : TimeSpan.FromMinutes(_options.ExpiryMinutes);
        var expires = DateTime.UtcNow.Add(lifetime);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(ClaimTypes.NameIdentifier, userId),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.Email, email),
            new("display_name", displayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (isSuperAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "SuperAdmin"));
            claims.Add(new Claim("role", "SuperAdmin"));
            claims.Add(new Claim("is_super_admin", "true"));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
