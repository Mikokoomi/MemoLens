using MemoLens.Models.Auth;
using Microsoft.Extensions.Options;

namespace MemoLens.Services.Auth;

public class RefreshTokenCleanupHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RefreshTokenCleanupOptions _options;
    private readonly ILogger<RefreshTokenCleanupHostedService> _logger;

    public RefreshTokenCleanupHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<RefreshTokenCleanupOptions> options,
        ILogger<RefreshTokenCleanupHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Refresh token cleanup is disabled by configuration.");
            return;
        }

        var interval = TimeSpan.FromHours(_options.CleanupIntervalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var cleanupService = scope.ServiceProvider.GetRequiredService<IRefreshTokenCleanupService>();

                await cleanupService.CleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    "Refresh token cleanup failed at {CleanupTimeUtc}. Exception type: {ExceptionType}.",
                    DateTime.UtcNow,
                    exception.GetType().Name);
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
