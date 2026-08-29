using DriveHarbor.Core.Configuration;
using DriveHarbor.Core.Drives;
using DriveHarbor.Core.Logging;
using DriveHarbor.Core.Robocopy;
using DriveHarbor.Core.Synchronization;
using DriveHarbor.Core.Tests.Infrastructure;
using DriveHarbor.Core.Validation;

namespace DriveHarbor.Core.Tests.Synchronization;

public sealed class SynchronizationServiceTests
{
    [Fact]
    public async Task DisconnectedDriveStopsBeforeRobocopy()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var oneDrive = temporaryDirectory.CreateDirectory("OneDrive");
        var destination = temporaryDirectory.CreateDirectory(Path.Combine("OneDrive", "Backup"));
        var runner = new StubRobocopyRunner();
        var service = CreateService([], [oneDrive], runner);
        var settings = CreateSettings(@"Z:\Source", destination, SyncMode.Backup);

        var result = await service.SynchronizeAsync(settings, mirrorConfirmed: false);

        Assert.Equal(SynchronizationStatus.SsdNotConnected, result.Status);
        Assert.Null(runner.LastRequest);
    }

    [Fact]
    public async Task DestinationOutsideOneDriveStopsBeforeRobocopy()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var mountedRoot = temporaryDirectory.CreateDirectory("Mounted");
        temporaryDirectory.CreateDirectory(Path.Combine("Mounted", "Source"));
        var destination = temporaryDirectory.CreateDirectory("OutsideOneDrive");
        var runner = new StubRobocopyRunner();
        var service = CreateService(
            [CreateVolume(mountedRoot)],
            [temporaryDirectory.CreateDirectory("OneDrive")],
            runner);
        var settings = CreateSettings(@"Z:\Source", destination, SyncMode.Backup);

        var result = await service.SynchronizeAsync(settings, mirrorConfirmed: false);

        Assert.Equal(SynchronizationStatus.DestinationUnavailable, result.Status);
        Assert.Null(runner.LastRequest);
    }

    [Fact]
    public async Task MirrorRequiresConfirmationBeforePreflightOrProcessStart()
    {
        var runner = new StubRobocopyRunner();
        var service = CreateService([], [], runner);
        var settings = CreateSettings(@"Z:\Source", @"C:\OneDrive\Backup", SyncMode.Mirror);

        var result = await service.SynchronizeAsync(settings, mirrorConfirmed: false);

        Assert.Equal(SynchronizationStatus.MirrorConfirmationRequired, result.Status);
        Assert.Null(runner.LastRequest);
    }

    [Fact]
    public async Task MirrorPreviewUsesDryRunAndNeverSetsExecutionConfirmation()
    {
        using var context = CreateValidContext(SyncMode.Mirror);

        var result = await context.Service.PreviewMirrorAsync(context.Settings);

        Assert.Equal(SynchronizationStatus.Completed, result.Status);
        Assert.True(context.Runner.LastRequest?.DryRun);
        Assert.False(context.Runner.LastRequest?.MirrorConfirmed);
    }

    [Fact]
    public async Task ConfirmedMirrorExecutionRechecksPathsAndPassesConfirmation()
    {
        using var context = CreateValidContext(SyncMode.Mirror);

        var result = await context.Service.SynchronizeAsync(context.Settings, mirrorConfirmed: true);

        Assert.Equal(SynchronizationStatus.Completed, result.Status);
        Assert.False(context.Runner.LastRequest?.DryRun);
        Assert.True(context.Runner.LastRequest?.MirrorConfirmed);
    }

    [Fact]
    public async Task BackupExecutionNeverSetsMirrorConfirmation()
    {
        using var context = CreateValidContext(SyncMode.Backup);

        var result = await context.Service.SynchronizeAsync(context.Settings, mirrorConfirmed: false);

        Assert.Equal(SynchronizationStatus.Completed, result.Status);
        Assert.Equal(SyncMode.Backup, context.Runner.LastRequest?.Mode);
        Assert.False(context.Runner.LastRequest?.MirrorConfirmed);
    }

    private static ValidContext CreateValidContext(SyncMode mode)
    {
        var temporaryDirectory = new TemporaryDirectory();
        var mountedRoot = temporaryDirectory.CreateDirectory("Mounted");
        temporaryDirectory.CreateDirectory(Path.Combine("Mounted", "Source"));
        var oneDrive = temporaryDirectory.CreateDirectory("OneDrive");
        var destination = temporaryDirectory.CreateDirectory(Path.Combine("OneDrive", "Backup"));
        var runner = new StubRobocopyRunner();
        var service = CreateService([CreateVolume(mountedRoot)], [oneDrive], runner);
        return new(
            temporaryDirectory,
            service,
            runner,
            CreateSettings(@"Z:\Source", destination, mode));
    }

    private static SynchronizationService CreateService(
        IReadOnlyList<VolumeDescriptor> volumes,
        IReadOnlyList<string> oneDriveRoots,
        StubRobocopyRunner runner) => new(
            new DriveDetectionService(new StubVolumeCatalog(volumes)),
            new PathSafetyValidator(new StubOneDriveRootProvider(oneDriveRoots)),
            runner,
            new StubLogger());

    private static AppSettings CreateSettings(string source, string destination, SyncMode mode) =>
        AppSettings.CreateDefault() with
        {
            SourcePath = source,
            DestinationPath = destination,
            Mode = mode,
            SourceDrive = new DriveFingerprint
            {
                VolumeSerialNumber = "1234-ABCD",
                VolumeLabel = "ARCHIVE",
            },
        };

    private static VolumeDescriptor CreateVolume(string root) => new(
        root,
        null,
        "1234-ABCD",
        "ARCHIVE",
        DriveType.Removable);

    private sealed class StubVolumeCatalog(IReadOnlyList<VolumeDescriptor> volumes) : IVolumeCatalog
    {
        public IReadOnlyList<VolumeDescriptor> GetAvailableVolumes() => volumes;
    }

    private sealed class StubOneDriveRootProvider(IReadOnlyList<string> roots) : IOneDriveRootProvider
    {
        public IReadOnlyList<string> GetAvailableRoots() => roots;
    }

    private sealed class StubRobocopyRunner : IRobocopyRunner
    {
        public RobocopyRequest? LastRequest { get; private set; }

        public Task<RobocopyResult> RunAsync(
            RobocopyRequest request,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(new RobocopyResult(
                RobocopyOperationStatus.Completed,
                0,
                "Completato",
                new(1, 0),
                ["summary"],
                []));
        }
    }

    private sealed class StubLogger : IAppLogger
    {
        public Task WriteAsync(
            LogLevel level,
            string message,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed record ValidContext(
        TemporaryDirectory TemporaryDirectory,
        SynchronizationService Service,
        StubRobocopyRunner Runner,
        AppSettings Settings) : IDisposable
    {
        public void Dispose() => TemporaryDirectory.Dispose();
    }
}
