namespace PayDefteri.Application.Common.Interfaces;

public interface IJwtTokenService
{
    (string AccessToken, DateTime ExpiresAtUtc) CreateToken(
        string userId,
        string email,
        string displayName,
        bool isSuperAdmin = false);
}
