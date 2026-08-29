using DriveHarbor.Core.Configuration;
using DriveHarbor.Core.Drives;
using DriveHarbor.Core.Tests.Infrastructure;

namespace DriveHarbor.Core.Tests.Drives;

public sealed class DriveDetectionServiceTests
{
    [Fact]
    public void CaptureStoresStableVolumeSignals()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var source = temporaryDirectory.CreateDirectory("Source");
        var root = Path.GetPathRoot(source)!;
        var volume = CreateVolume(root, "guid-a", "1234-ABCD", "ARCHIVE");
        var service = CreateService(volume);

        var result = service.Capture(source);

        Assert.Equal(DriveCaptureStatus.Captured, result.Status);
        Assert.Equal("guid-a", result.Fingerprint?.VolumeGuidPath);
        Assert.Equal("1234-ABCD", result.Fingerprint?.VolumeSerialNumber);
        Assert.Equal("ARCHIVE", result.Fingerprint?.VolumeLabel);
    }

    [Fact]
    public void CaptureRejectsLabelOnlyIdentity()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var source = temporaryDirectory.CreateDirectory("Source");
        var root = Path.GetPathRoot(source)!;
        var service = CreateService(CreateVolume(root, null, null, "ARCHIVE"));

        var result = service.Capture(source);

        Assert.Equal(DriveCaptureStatus.StableIdentityUnavailable, result.Status);
        Assert.Null(result.Fingerprint);
    }

    [Fact]
    public void MissingFingerprintIsNotConfigured()
    {
        var service = CreateService();

        var result = service.Resolve(@"E:\Archive", null);

        Assert.Equal(DriveConnectionStatus.NotConfigured, result.Status);
    }

    [Fact]
    public void VolumeGuidResolvesSourceAfterDriveLetterChange()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var currentRoot = temporaryDirectory.CreateDirectory("MountedVolume");
        var expectedSource = temporaryDirectory.CreateDirectory(Path.Combine("MountedVolume", "Archive", "Photos"));
        var volume = CreateVolume(currentRoot, "guid-a", "1234-ABCD", "ARCHIVE");
        var fingerprint = CreateFingerprint("guid-a", "1234-ABCD", "ARCHIVE");
        var service = CreateService(volume);

        var result = service.Resolve(@"Z:\Archive\Photos", fingerprint);

        Assert.Equal(DriveConnectionStatus.Connected, result.Status);
        Assert.Equal(expectedSource, result.ResolvedSourcePath);
    }

    [Fact]
    public void DifferentVolumeGuidIsDisconnectedEvenWhenSerialMatches()
    {
        var volume = CreateVolume(@"E:\", "guid-other", "1234-ABCD", "ARCHIVE");
        var fingerprint = CreateFingerprint("guid-configured", "1234-ABCD", "ARCHIVE");
        var service = CreateService(volume);

        var result = service.Resolve(@"Z:\Archive", fingerprint);

        Assert.Equal(DriveConnectionStatus.Disconnected, result.Status);
    }

    [Fact]
    public void SerialFallbackWorksWhenCurrentGuidCannotBeRead()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var currentRoot = temporaryDirectory.CreateDirectory("MountedVolume");
        temporaryDirectory.CreateDirectory(Path.Combine("MountedVolume", "Archive"));
        var volume = CreateVolume(currentRoot, null, "1234-ABCD", "ARCHIVE");
        var fingerprint = CreateFingerprint("guid-configured", "1234-ABCD", "ARCHIVE");
        var service = CreateService(volume);

        var result = service.Resolve(@"Z:\Archive", fingerprint);

        Assert.Equal(DriveConnectionStatus.Connected, result.Status);
    }

    [Fact]
    public void DuplicateSerialWithoutDisambiguationIsAmbiguous()
    {
        var fingerprint = CreateFingerprint(null, "1234-ABCD", null);
        var service = CreateService(
            CreateVolume(@"E:\", null, "1234-ABCD", "FIRST"),
            CreateVolume(@"F:\", null, "1234-ABCD", "SECOND"));

        var result = service.Resolve(@"Z:\Archive", fingerprint);

        Assert.Equal(DriveConnectionStatus.Ambiguous, result.Status);
    }

    [Fact]
    public void LabelDisambiguatesDuplicateSerial()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var currentRoot = temporaryDirectory.CreateDirectory("ExpectedVolume");
        temporaryDirectory.CreateDirectory(Path.Combine("ExpectedVolume", "Archive"));
        var fingerprint = CreateFingerprint(null, "1234-ABCD", "SECOND");
        var service = CreateService(
            CreateVolume(@"E:\", null, "1234-ABCD", "FIRST"),
            CreateVolume(currentRoot, null, "1234-ABCD", "SECOND"));

        var result = service.Resolve(@"Z:\Archive", fingerprint);

        Assert.Equal(DriveConnectionStatus.Connected, result.Status);
        Assert.Equal("SECOND", result.Volume?.VolumeLabel);
    }

    [Fact]
    public void MissingConfiguredFolderIsReportedSeparatelyFromDisconnectedDrive()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var currentRoot = temporaryDirectory.CreateDirectory("MountedVolume");
        var volume = CreateVolume(currentRoot, "guid-a", "1234-ABCD", "ARCHIVE");
        var service = CreateService(volume);

        var result = service.Resolve(
            @"Z:\MissingFolder",
            CreateFingerprint("guid-a", "1234-ABCD", "ARCHIVE"));

        Assert.Equal(DriveConnectionStatus.SourceFolderUnavailable, result.Status);
        Assert.NotNull(result.Volume);
    }

    private static DriveDetectionService CreateService(params VolumeDescriptor[] volumes) =>
        new(new StubVolumeCatalog(volumes));

    private static VolumeDescriptor CreateVolume(
        string root,
        string? guid,
        string? serial,
        string? label) => new(root, guid, serial, label, DriveType.Removable);

    private static DriveFingerprint CreateFingerprint(
        string? guid,
        string? serial,
        string? label) => new()
        {
            VolumeGuidPath = guid,
            VolumeSerialNumber = serial,
            VolumeLabel = label,
        };

    private sealed class StubVolumeCatalog(IReadOnlyList<VolumeDescriptor> volumes) : IVolumeCatalog
    {
        public IReadOnlyList<VolumeDescriptor> GetAvailableVolumes() => volumes;
    }
}
