namespace DriveHarbor.Core.Tests;

public sealed class ProjectConventionsTests
{
    [Fact]
    public void TestsTargetDotNetTen()
    {
        Assert.StartsWith("10.", Environment.Version.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ReadOnlyUpdateProgressBindingIsOneWay()
    {
        var repositoryRoot = FindRepositoryRoot();
        var xaml = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "DriveHarbor.App",
            "MainWindow.xaml"));

        Assert.Contains(
            "Value=\"{Binding UpdateDownloadPercentage, Mode=OneWay}\"",
            xaml,
            StringComparison.Ordinal);
    }

    [Fact]
    public void StatusStylesCompareAvailabilityAsBooleanBindings()
    {
        var repositoryRoot = FindRepositoryRoot();
        var xaml = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "DriveHarbor.App",
            "MainWindow.xaml"));

        const string booleanStatusTrigger =
            "<DataTrigger Binding=\"{Binding Tag, RelativeSource={RelativeSource Self}}\" Value=\"True\">";

        Assert.Equal(2, xaml.Split(booleanStatusTrigger, StringSplitOptions.None).Length - 1);
        Assert.DoesNotContain("<Trigger Property=\"Tag\" Value=\"True\">", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void DarkThemeControlsUseApplicationPaletteInsteadOfSystemSelectionColors()
    {
        var repositoryRoot = FindRepositoryRoot();
        var xaml = File.ReadAllText(Path.Combine(repositoryRoot, "src", "DriveHarbor.App", "MainWindow.xaml"));
        var themeService = File.ReadAllText(Path.Combine(
            repositoryRoot, "src", "DriveHarbor.App", "Services", "ThemeService.cs"));

        Assert.Contains("DynamicResource ControlHoverBrush", xaml, StringComparison.Ordinal);
        Assert.Contains("DynamicResource ControlSelectedBrush", xaml, StringComparison.Ordinal);
        Assert.Contains("SetBrush(\"ControlHoverBrush\", dark ?", themeService, StringComparison.Ordinal);
        Assert.Contains("SetBrush(\"ControlSelectedBrush\", dark ?", themeService, StringComparison.Ordinal);
    }

    [Fact]
    public void DarkThemeIsAppliedToTheNativeWindowTitleBar()
    {
        var repositoryRoot = FindRepositoryRoot();
        var themeService = File.ReadAllText(Path.Combine(
            repositoryRoot, "src", "DriveHarbor.App", "Services", "ThemeService.cs"));

        Assert.Contains("ApplyWindowTitleBars(useDarkColors);", themeService, StringComparison.Ordinal);
        Assert.Contains("DwmwaUseImmersiveDarkMode = 20", themeService, StringComparison.Ordinal);
        Assert.Contains("DwmSetWindowAttribute(", themeService, StringComparison.Ordinal);
    }

    [Fact]
    public void SynchronizationCancelActionUsesDangerStyling()
    {
        var repositoryRoot = FindRepositoryRoot();
        var xaml = File.ReadAllText(Path.Combine(repositoryRoot, "src", "DriveHarbor.App", "MainWindow.xaml"));

        Assert.Contains("x:Key=\"DangerButton\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource DangerButton}\" Command=\"{Binding CancelCommand}\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void SidebarBrandDisplaysThePackagedApplicationLogo()
    {
        var repositoryRoot = FindRepositoryRoot();
        var xaml = File.ReadAllText(Path.Combine(repositoryRoot, "src", "DriveHarbor.App", "MainWindow.xaml"));

        Assert.Contains("Source=\"Assets/DriveHarbor.ico\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void ActiveSynchronizationIsCancelledWhenTheSourceDisconnects()
    {
        var repositoryRoot = FindRepositoryRoot();
        var viewModel = File.ReadAllText(Path.Combine(
            repositoryRoot, "src", "DriveHarbor.App", "ViewModels", "MainViewModel.cs"));

        Assert.Contains("CancelSynchronizationForDisconnectedSource();", viewModel, StringComparison.Ordinal);
        Assert.Contains("SynchronizationStatus.SourceDisconnected", viewModel, StringComparison.Ordinal);
        Assert.Contains("Interrotta: SSD scollegato", viewModel, StringComparison.Ordinal);
    }

    [Fact]
    public void BackgroundSettingsAndTrayActionsAreExposedToTheUser()
    {
        var repositoryRoot = FindRepositoryRoot();
        var xaml = File.ReadAllText(Path.Combine(repositoryRoot, "src", "DriveHarbor.App", "MainWindow.xaml"));
        var trayService = File.ReadAllText(Path.Combine(
            repositoryRoot, "src", "DriveHarbor.App", "Services", "TrayIconService.cs"));
        var startupService = File.ReadAllText(Path.Combine(
            repositoryRoot, "src", "DriveHarbor.App", "Services", "WindowsStartupRegistrationService.cs"));

        Assert.Contains("IsChecked=\"{Binding RunAtWindowsStartup}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsChecked=\"{Binding KeepRunningInBackground}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsChecked=\"{Binding StartMinimizedToTray}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Apri DriveHarbor", trayService, StringComparison.Ordinal);
        Assert.Contains("Sincronizza ora", trayService, StringComparison.Ordinal);
        Assert.Contains("--background", startupService, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DriveHarbor.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("DriveHarbor repository root not found.");
    }
}
