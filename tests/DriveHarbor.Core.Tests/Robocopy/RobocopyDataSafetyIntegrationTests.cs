using System.Security.Cryptography;
using DriveHarbor.Core.Configuration;
using DriveHarbor.Core.Robocopy;
using DriveHarbor.Core.Tests.Infrastructure;

namespace DriveHarbor.Core.Tests.Robocopy;

public sealed class RobocopyDataSafetyIntegrationTests
{
    [Fact]
    public async Task BackupCopiesChangesKeepsDestinationExtrasAndNeverChangesSource()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var source = temporaryDirectory.CreateDirectory("BackupSource");
        var destination = temporaryDirectory.CreateDirectory("BackupDestination");
        await WriteFixtureAsync(source, "current.txt", "new source content");
        await WriteFixtureAsync(source, Path.Combine("Nested", "photo.jpg"), "photo bytes");
        await WriteFixtureAsync(destination, "current.txt", "old destination content");
        await WriteFixtureAsync(destination, "destination-only.txt", "must remain");
        var sourceBefore = CreateSnapshot(source);
        var runner = new RobocopyRunner();

        var result = await runner.RunAsync(new(source, destination, SyncMode.Backup));

        Assert.NotEqual(RobocopyOperationStatus.Failed, result.Status);
        Assert.NotEqual(RobocopyOperationStatus.Cancelled, result.Status);
        Assert.Equal("new source content", await File.ReadAllTextAsync(Path.Combine(destination, "current.txt")));
        Assert.Equal("photo bytes", await File.ReadAllTextAsync(Path.Combine(destination, "Nested", "photo.jpg")));
        Assert.True(File.Exists(Path.Combine(destination, "destination-only.txt")));
        Assert.Equal(sourceBefore, CreateSnapshot(source));
    }

    [Fact]
    public async Task ConfirmedMirrorDeletesOnlyDestinationExtrasAndNeverChangesSource()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var source = temporaryDirectory.CreateDirectory("MirrorSource");
        var destination = temporaryDirectory.CreateDirectory("MirrorDestination");
        await WriteFixtureAsync(source, "current.txt", "source version");
        await WriteFixtureAsync(source, Path.Combine("Nested", "keep.bin"), "binary fixture");
        await WriteFixtureAsync(destination, "current.txt", "outdated version");
        await WriteFixtureAsync(destination, "obsolete.txt", "must be deleted from destination");
        var sourceBefore = CreateSnapshot(source);
        var runner = new RobocopyRunner();

        var result = await runner.RunAsync(new(
            source,
            destination,
            SyncMode.Mirror,
            MirrorConfirmed: true));

        Assert.NotEqual(RobocopyOperationStatus.Failed, result.Status);
        Assert.NotEqual(RobocopyOperationStatus.Cancelled, result.Status);
        Assert.False(File.Exists(Path.Combine(destination, "obsolete.txt")));
        Assert.Equal(sourceBefore, CreateSnapshot(source));
        Assert.Equal(sourceBefore, CreateSnapshot(destination));
    }

    private static async Task WriteFixtureAsync(string root, string relativePath, string content)
    {
        var path = Path.Combine(root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, content);
    }

    private static string[] CreateSnapshot(string root) =>
        Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(path =>
                $"{Path.GetRelativePath(root, path)}|{Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))}")
            .OrderBy(entry => entry, StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
