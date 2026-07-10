namespace MemoLens.Services.Auth;

public interface IRefreshTokenCleanupService
{
    Task<int> CleanupAsync(CancellationToken cancellationToken = default);
}
