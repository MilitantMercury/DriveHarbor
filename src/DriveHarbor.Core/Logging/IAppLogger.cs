namespace DriveHarbor.Core.Logging;

public interface IAppLogger
{
    Task WriteAsync(LogLevel level, string message, CancellationToken cancellationToken = default);
}
