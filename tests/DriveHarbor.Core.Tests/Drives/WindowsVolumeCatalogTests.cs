using DriveHarbor.Core.Drives;

namespace DriveHarbor.Core.Tests.Drives;

public sealed class WindowsVolumeCatalogTests
{
    [Fact]
    public void AvailableVolumeSnapshotCanBeReadWithoutWritingToDrives()
    {
        var catalog = new WindowsVolumeCatalog();

        var volumes = catalog.GetAvailableVolumes();

        Assert.NotNull(volumes);
        Assert.All(volumes, volume => Assert.True(Path.IsPathFullyQualified(volume.RootPath)));
    }
}
