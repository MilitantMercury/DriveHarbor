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
