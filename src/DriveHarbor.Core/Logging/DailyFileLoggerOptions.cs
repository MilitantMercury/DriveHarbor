using DriveHarbor.Core.Configuration;

namespace DriveHarbor.Core.Logging;

public sealed record DailyFileLoggerOptions
{
    public string DirectoryPath { get; init; } = AppPaths.DefaultLogDirectory;

    public TimeSpan Retention { get; init; } = TimeSpan.FromDays(30);

    public long MaximumTotalBytes { get; init; } = 100 * 1024 * 1024;

    public long MaximumFileBytes { get; init; } = 10 * 1024 * 1024;
}
