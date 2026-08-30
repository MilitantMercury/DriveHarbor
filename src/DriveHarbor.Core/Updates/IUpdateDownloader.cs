namespace DriveHarbor.Core.Updates;

public interface IUpdateDownloader
{
    Task<UpdateDownloadResult> DownloadAsync(UpdateCheckResult update, CancellationToken cancellationToken = default);
}
