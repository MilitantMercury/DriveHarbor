namespace DriveHarbor.Core.Updates;

public interface IUpdateChecker
{
    Task<UpdateCheckResult> CheckAsync(string currentVersion, UpdateChannel channel, CancellationToken cancellationToken = default);
}
