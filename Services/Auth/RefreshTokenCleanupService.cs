using MemoLens.Data;
using MemoLens.Models.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MemoLens.Services.Auth;

public class RefreshTokenCleanupService : IRefreshTokenCleanupService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly RefreshTokenCleanupOptions _options;
    private readonly ILogger<RefreshTokenCleanupService> _logger;

    public RefreshTokenCleanupService(
        ApplicationDbContext dbContext,
        IOptions<RefreshTokenCleanupOptions> options,
        ILogger<RefreshTokenCleanupService> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> CleanupAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var revokedCutoff = now.AddDays(-_options.RevokedTokenRetentionDays);
        var expiredCutoff = now.AddDays(-_options.ExpiredTokenRetentionDays);
        var deletedCount = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var tokenIds = await _dbContext.UserRefreshTokens
                .Where(token =>
                    (token.RevokedAt.HasValue && token.RevokedAt.Value < revokedCutoff) ||
                    token.ExpiresAt < expiredCutoff)
                .OrderBy(token => token.Id)
                .Select(token => token.Id)
                .Take(_options.BatchSize)
                .ToListAsync(cancellationToken);

            if (tokenIds.Count == 0)
            {
                break;
            }

            deletedCount += await _dbContext.UserRefreshTokens
                .Where(token =>
                    tokenIds.Contains(token.Id) &&
                    ((token.RevokedAt.HasValue && token.RevokedAt.Value < revokedCutoff) ||
                     token.ExpiresAt < expiredCutoff))
                .ExecuteDeleteAsync(cancellationToken);

            if (tokenIds.Count < _options.BatchSize)
            {
                break;
            }
        }

        if (deletedCount > 0)
        {
            _logger.LogInformation(
                "Refresh token cleanup removed {DeletedCount} records at {CleanupTimeUtc}.",
                deletedCount,
                now);
        }

        return deletedCount;
    }
}
