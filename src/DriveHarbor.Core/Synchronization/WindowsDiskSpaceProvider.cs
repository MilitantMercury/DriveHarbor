namespace DriveHarbor.Core.Synchronization;

public sealed class WindowsDiskSpaceProvider : IDiskSpaceProvider
{
    public long? GetAvailableBytes(string path)
    {
        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(path));
            return string.IsNullOrWhiteSpace(root) ? null : new DriveInfo(root).AvailableFreeSpace;
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or IOException
                or UnauthorizedAccessException
                or DriveNotFoundException)
        {
            return null;
        }
    }
}
