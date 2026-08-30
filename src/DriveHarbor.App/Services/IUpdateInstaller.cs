namespace DriveHarbor.App.Services;

public interface IUpdateInstaller
{
    bool TryStart(string packagePath, out string? errorMessage);
}
