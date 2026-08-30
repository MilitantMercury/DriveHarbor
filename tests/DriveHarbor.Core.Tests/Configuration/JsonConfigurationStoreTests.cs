using DriveHarbor.Core.Configuration;
using DriveHarbor.Core.Tests.Infrastructure;
using DriveHarbor.Core.Updates;

namespace DriveHarbor.Core.Tests.Configuration;

public sealed class JsonConfigurationStoreTests
{
    [Fact]
    public async Task MissingFileReturnsSafeDefaults()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var store = CreateStore(temporaryDirectory);

        var result = await store.LoadAsync();

        Assert.Equal(ConfigurationLoadStatus.DefaultsUsed, result.Status);
        Assert.Equal(SyncMode.Backup, result.Settings.Mode);
        Assert.Null(result.Settings.SourcePath);
        Assert.Null(result.Settings.DestinationPath);
    }

    [Fact]
    public async Task SaveAndLoadRoundTripsSettings()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.FullPath, "settings.json");
        var store = new JsonConfigurationStore(settingsPath);
        var expected = AppSettings.CreateDefault() with
        {
            SourcePath = @"E:\Media",
            DestinationPath = @"C:\OneDrive\Media",
            Mode = SyncMode.Mirror,
            Theme = AppTheme.Dark,
            UpdateChannel = UpdateChannel.Beta,
            SourceDrive = new DriveFingerprint
            {
                VolumeGuidPath = @"\\?\Volume{11111111-1111-1111-1111-111111111111}\",
                VolumeSerialNumber = "1234-ABCD",
                VolumeLabel = "ARCHIVE",
            },
            Exclusions = ["*.tmp", "Cache"],
        };

        await store.SaveAsync(expected);
        var result = await store.LoadAsync();

        Assert.Equal(ConfigurationLoadStatus.Loaded, result.Status);
        Assert.Equal(expected.SourcePath, result.Settings.SourcePath);
        Assert.Equal(expected.DestinationPath, result.Settings.DestinationPath);
        Assert.Equal(expected.Mode, result.Settings.Mode);
        Assert.Equal(expected.Theme, result.Settings.Theme);
        Assert.Equal(expected.UpdateChannel, result.Settings.UpdateChannel);
        Assert.Equal(expected.SourceDrive, result.Settings.SourceDrive);
        Assert.Equal(expected.Exclusions, result.Settings.Exclusions);
        Assert.Empty(Directory.EnumerateFiles(temporaryDirectory.FullPath, "*.tmp"));
    }

    [Fact]
    public async Task InvalidJsonIsNotOverwritten()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.FullPath, "settings.json");
        const string invalidJson = "{ not-valid-json";
        await File.WriteAllTextAsync(settingsPath, invalidJson);
        var store = new JsonConfigurationStore(settingsPath);

        var result = await store.LoadAsync();

        Assert.Equal(ConfigurationLoadStatus.InvalidFile, result.Status);
        Assert.Equal(SyncMode.Backup, result.Settings.Mode);
        Assert.Equal(invalidJson, await File.ReadAllTextAsync(settingsPath));
    }

    [Fact]
    public async Task UnsupportedSchemaReturnsSafeDefaultsWithoutChangingFile()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.FullPath, "settings.json");
        const string futureConfiguration = """
            {
              "schemaVersion": 999,
              "mode": "Mirror"
            }
            """;
        await File.WriteAllTextAsync(settingsPath, futureConfiguration);
        var store = new JsonConfigurationStore(settingsPath);

        var result = await store.LoadAsync();

        Assert.Equal(ConfigurationLoadStatus.UnsupportedVersion, result.Status);
        Assert.Equal(SyncMode.Backup, result.Settings.Mode);
        Assert.Equal(futureConfiguration, await File.ReadAllTextAsync(settingsPath));
    }

    private static JsonConfigurationStore CreateStore(TemporaryDirectory temporaryDirectory) =>
        new(Path.Combine(temporaryDirectory.FullPath, "settings.json"));
}
