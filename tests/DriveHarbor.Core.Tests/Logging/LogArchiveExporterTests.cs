using System.IO.Compression;
using DriveHarbor.Core.Logging;
using DriveHarbor.Core.Tests.Infrastructure;

namespace DriveHarbor.Core.Tests.Logging;

public sealed class LogArchiveExporterTests
{
    [Fact]
    public void ExportCreatesArchiveWithOnlyLogFiles()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var logs = temporaryDirectory.CreateDirectory("Logs");
        File.WriteAllText(Path.Combine(logs, "DriveHarbor-2026-08-30.log"), "first");
        File.WriteAllText(Path.Combine(logs, "DriveHarbor-2026-08-31.log"), "second");
        File.WriteAllText(Path.Combine(logs, "settings.json"), "private settings");
        var archivePath = Path.Combine(temporaryDirectory.FullPath, "export.zip");

        var count = LogArchiveExporter.Export(logs, archivePath);

        Assert.Equal(2, count);
        using var archive = ZipFile.OpenRead(archivePath);
        Assert.Equal(
            ["DriveHarbor-2026-08-30.log", "DriveHarbor-2026-08-31.log"],
            archive.Entries.Select(entry => entry.FullName).Order().ToArray());
    }

    [Fact]
    public void ExportWithNoLogsDoesNotCreateArchive()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var logs = temporaryDirectory.CreateDirectory("Logs");
        var archivePath = Path.Combine(temporaryDirectory.FullPath, "export.zip");

        var count = LogArchiveExporter.Export(logs, archivePath);

        Assert.Equal(0, count);
        Assert.False(File.Exists(archivePath));
    }
}
