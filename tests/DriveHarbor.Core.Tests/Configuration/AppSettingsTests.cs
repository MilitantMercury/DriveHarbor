using DriveHarbor.Core.Configuration;

namespace DriveHarbor.Core.Tests.Configuration;

public sealed class AppSettingsTests
{
    [Fact]
    public void DefaultsUseBackupModeAndSafeExclusions()
    {
        var settings = AppSettings.CreateDefault();

        Assert.Equal(SyncMode.Backup, settings.Mode);
        Assert.Equal(AppTheme.System, settings.Theme);
        Assert.Contains("$RECYCLE.BIN", settings.Exclusions);
        Assert.Contains("System Volume Information", settings.Exclusions);
        Assert.Contains("*.tmp", settings.Exclusions);
        Assert.DoesNotContain("*.docx", settings.Exclusions);
        Assert.DoesNotContain("*.jpg", settings.Exclusions);
    }
}
