namespace DriveHarbor.Core.Updates;

public interface IUpdateDownloader
{
    Task<UpdateDownloadResult> DownloadAsync(
        UpdateCheckResult update,
        IProgress<UpdateDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
