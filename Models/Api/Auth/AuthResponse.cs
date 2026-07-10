namespace MemoLens.Models.Api.Auth;

public class AuthResponse
{
    public string AccessToken { get; init; } = string.Empty;

    public string RefreshToken { get; init; } = string.Empty;

    public int ExpiresInSeconds { get; init; }

    public string TokenType { get; init; } = "Bearer";

    public UserSummaryResponse User { get; init; } = new();
}
