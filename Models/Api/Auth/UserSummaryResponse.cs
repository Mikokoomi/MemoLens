namespace MemoLens.Models.Api.Auth;

public class UserSummaryResponse
{
    public string Id { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? DisplayName { get; init; }

    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
}
