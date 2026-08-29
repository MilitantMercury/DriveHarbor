using System.Text;

namespace DriveHarbor.Core.Logging;

public sealed class DailyFileLogger(
    DailyFileLoggerOptions options,
    TimeProvider? timeProvider = null) : IAppLogger, IDisposable
{
    private readonly SemaphoreSlim writeLock = new(1, 1);
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;

    public async Task WriteAsync(
        LogLevel level,
        string message,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ValidateOptions();

        await writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(options.DirectoryPath);
            PurgeExpiredAndOversizedLogs();

            var now = clock.GetLocalNow();
            var logPath = FindWritableLogPath(now.Date);
            var safeMessage = message.ReplaceLineEndings(" ");
            var line = $"{now:O} [{level}] {safeMessage}{Environment.NewLine}";
            await File.AppendAllTextAsync(logPath, line, Encoding.UTF8, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            writeLock.Release();
        }
    }

    public void Dispose() => writeLock.Dispose();

    private string FindWritableLogPath(DateTimeOffset date)
    {
        for (var sequence = 1; ; sequence++)
        {
            var suffix = sequence == 1 ? string.Empty : $"-{sequence}";
            var path = Path.Combine(options.DirectoryPath, $"DriveHarbor-{date:yyyy-MM-dd}{suffix}.log");
            if (!File.Exists(path) || new FileInfo(path).Length < options.MaximumFileBytes)
            {
                return path;
            }
        }
    }

    private void PurgeExpiredAndOversizedLogs()
    {
        var files = new DirectoryInfo(options.DirectoryPath)
            .EnumerateFiles("DriveHarbor-*.log", SearchOption.TopDirectoryOnly)
            .OrderBy(file => file.LastWriteTimeUtc)
            .ToList();
        var expirationThreshold = clock.GetUtcNow() - options.Retention;

        foreach (var file in files.Where(file => file.LastWriteTimeUtc < expirationThreshold.UtcDateTime).ToArray())
        {
            file.Delete();
            files.Remove(file);
        }

        var totalBytes = files.Sum(file => file.Length);
        foreach (var file in files)
        {
            if (totalBytes <= options.MaximumTotalBytes)
            {
                break;
            }

            totalBytes -= file.Length;
            file.Delete();
        }
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(options.DirectoryPath)
            || options.Retention <= TimeSpan.Zero
            || options.MaximumTotalBytes <= 0
            || options.MaximumFileBytes <= 0
            || options.MaximumFileBytes > options.MaximumTotalBytes)
        {
            throw new InvalidOperationException("The log retention options are invalid.");
        }
    }
}
