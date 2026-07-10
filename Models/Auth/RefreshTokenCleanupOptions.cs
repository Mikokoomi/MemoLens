namespace MemoLens.Models.Auth;

public class RefreshTokenCleanupOptions
{
    public const string SectionName = "RefreshTokenCleanup";

    public bool Enabled { get; set; }

    public int CleanupIntervalHours { get; set; } = 24;

    public int RevokedTokenRetentionDays { get; set; } = 30;

    public int ExpiredTokenRetentionDays { get; set; } = 30;

    public int BatchSize { get; set; } = 500;
}
