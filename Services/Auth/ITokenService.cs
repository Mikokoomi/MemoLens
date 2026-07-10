using MemoLens.Models;

namespace MemoLens.Services.Auth;

public interface ITokenService
{
    Task<string> GenerateAccessTokenAsync(ApplicationUser user);

    string GenerateRefreshToken();

    string HashRefreshToken(string refreshToken);

    bool ValidateRefreshTokenHash(string refreshToken, string tokenHash);
}
