namespace PayDefteri.Application.Common.Interfaces;

public interface IMobileRefreshTokenService
{
    string CreateToken();
    string HashToken(string token);
    DateTime GetExpiryUtc();
}
