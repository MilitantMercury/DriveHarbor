namespace DriveHarbor.Core.Validation;

public interface IOneDriveRootProvider
{
    IReadOnlyList<string> GetAvailableRoots();
}
