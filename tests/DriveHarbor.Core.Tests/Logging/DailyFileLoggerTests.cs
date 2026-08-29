using DriveHarbor.Core.Logging;
using DriveHarbor.Core.Tests.Infrastructure;

namespace DriveHarbor.Core.Tests.Logging;

public sealed class DailyFileLoggerTests
{
    [Fact]
    public async Task MessagesAreWrittenToDatedLocalFileWithoutInjectedNewlines()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var clock = new FixedTimeProvider(new(2026, 8, 29, 12, 0, 0, TimeSpan.Zero));
        using var logger = CreateLogger(temporaryDirectory, clock);

        await logger.WriteAsync(LogLevel.Information, "first line\r\nforged line");

        var logPath = Assert.Single(Directory.EnumerateFiles(temporaryDirectory.FullPath, "*.log"));
        Assert.EndsWith("DriveHarbor-2026-08-29.log", logPath, StringComparison.Ordinal);
        var lines = await File.ReadAllLinesAsync(logPath);
        Assert.Single(lines);
        Assert.Contains("first line forged line", lines[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExpiredLogsAreRemovedBeforeWriting()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var oldLog = Path.Combine(temporaryDirectory.FullPath, "DriveHarbor-2026-01-01.log");
        await File.WriteAllTextAsync(oldLog, "old");
        File.SetLastWriteTimeUtc(oldLog, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var clock = new FixedTimeProvider(new(2026, 8, 29, 12, 0, 0, TimeSpan.Zero));
        using var logger = CreateLogger(temporaryDirectory, clock);

        await logger.WriteAsync(LogLevel.Information, "current");

        Assert.False(File.Exists(oldLog));
        Assert.Single(Directory.EnumerateFiles(temporaryDirectory.FullPath, "*.log"));
    }

    [Fact]
    public async Task FullDailyLogContinuesInNumberedFile()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var clock = new FixedTimeProvider(new(2026, 8, 29, 12, 0, 0, TimeSpan.Zero));
        var options = new DailyFileLoggerOptions
        {
            DirectoryPath = temporaryDirectory.FullPath,
            Retention = TimeSpan.FromDays(30),
            MaximumFileBytes = 32,
            MaximumTotalBytes = 1024,
        };
        using var logger = new DailyFileLogger(options, clock);

        await logger.WriteAsync(LogLevel.Information, "a message longer than the configured file size");
        await logger.WriteAsync(LogLevel.Information, "second message");

        Assert.Equal(2, Directory.EnumerateFiles(temporaryDirectory.FullPath, "*.log").Count());
    }

    private static DailyFileLogger CreateLogger(
        TemporaryDirectory temporaryDirectory,
        TimeProvider clock) => new(
            new DailyFileLoggerOptions
            {
                DirectoryPath = temporaryDirectory.FullPath,
                Retention = TimeSpan.FromDays(30),
                MaximumFileBytes = 1024,
                MaximumTotalBytes = 4096,
            },
            clock);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
