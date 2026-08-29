namespace DriveHarbor.Core.Synchronization;

public interface IDiskSpaceProvider
{
    long? GetAvailableBytes(string path);
}
