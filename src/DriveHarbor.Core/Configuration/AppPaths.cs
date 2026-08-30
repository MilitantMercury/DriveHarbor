namespace DriveHarbor.Core.Configuration;

public static class AppPaths
{
    private const string ApplicationDirectoryName = "DriveHarbor";

    public static string LocalDataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        ApplicationDirectoryName);

    public static string DefaultSettingsFile => Path.Combine(LocalDataDirectory, "settings.json");

    public static string DefaultLogDirectory => Path.Combine(LocalDataDirectory, "Logs");

    public static string UpdatesDirectory => Path.Combine(LocalDataDirectory, "Updates");
}
