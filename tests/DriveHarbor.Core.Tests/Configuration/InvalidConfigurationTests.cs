using DriveHarbor.Core.Configuration;
using DriveHarbor.Core.Tests.Infrastructure;

namespace DriveHarbor.Core.Tests.Configuration;

public sealed class InvalidConfigurationTests
{
    [Theory]
    [InlineData("{\"schemaVersion\":1,\"mode\":999,\"exclusions\":[],\"logDirectory\":\"Logs\"}")]
    [InlineData("{\"schemaVersion\":1,\"mode\":\"Backup\",\"exclusions\":null,\"logDirectory\":\"Logs\"}")]
    [InlineData("{\"schemaVersion\":1,\"mode\":\"Backup\",\"exclusions\":[],\"logDirectory\":\" \"}")]
    public async Task StructurallyInvalidSettingsReturnSafeDefaults(string json)
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.FullPath, "settings.json");
        await File.WriteAllTextAsync(settingsPath, json);
        var store = new JsonConfigurationStore(settingsPath);

        var result = await store.LoadAsync();

        Assert.Equal(ConfigurationLoadStatus.InvalidFile, result.Status);
        Assert.Equal(SyncMode.Backup, result.Settings.Mode);
        Assert.Equal(json, await File.ReadAllTextAsync(settingsPath));
    }
}
