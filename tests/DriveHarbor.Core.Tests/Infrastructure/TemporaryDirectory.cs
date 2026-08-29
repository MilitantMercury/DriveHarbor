namespace DriveHarbor.Core.Tests.Infrastructure;

internal sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory()
    {
        FullPath = Path.Combine(Path.GetTempPath(), "DriveHarbor.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(FullPath);
    }

    public string FullPath { get; }

    public string CreateDirectory(string relativePath)
    {
        var path = Path.Combine(FullPath, relativePath);
        Directory.CreateDirectory(path);
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(FullPath))
        {
            Directory.Delete(FullPath, recursive: true);
        }
    }
}
