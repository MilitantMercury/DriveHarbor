using System.Net.Http;
using System.IO;
using System.Windows;
using DriveHarbor.App.Services;
using DriveHarbor.App.ViewModels;
using DriveHarbor.Core.Configuration;
using DriveHarbor.Core.Drives;
using DriveHarbor.Core.Robocopy;
using DriveHarbor.Core.Updates;
using DriveHarbor.Core.Validation;

namespace DriveHarbor.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application, IDisposable
{
    private MainViewModel? mainViewModel;
    private ThemeService? themeService;
    private TrayIconService? trayIconService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var driveDetection = new DriveDetectionService(new WindowsVolumeCatalog());
        var pathValidator = new PathSafetyValidator(new EnvironmentOneDriveRootProvider());
        themeService = new();
        mainViewModel = new(
            new JsonConfigurationStore(),
            driveDetection,
            pathValidator,
            new RobocopyRunner(),
            new FolderPicker(),
            new UserDialog(),
            themeService,
            new GitHubUpdateChecker(new HttpClient()),
            new VerifiedUpdateDownloader(new HttpClient(), AppPaths.UpdatesDirectory),
            new SelfUpdateInstaller(),
            new WindowsStartupRegistrationService());

        var startedFromWindows = e.Args.Contains("--background", StringComparer.OrdinalIgnoreCase);
        var window = new MainWindow(mainViewModel, startedFromWindows);
        MainWindow = window;
        trayIconService = new(window, mainViewModel);
        window.AttachTrayIcon(trayIconService);
        window.Show();
        ShowLastUpdateResult();
    }

    private static void ShowLastUpdateResult()
    {
        if (!File.Exists(AppPaths.UpdateResultFile)) return;
        try
        {
            var lines = File.ReadAllLines(AppPaths.UpdateResultFile);
            File.Delete(AppPaths.UpdateResultFile);
            var message = lines.Length > 1 ? string.Join(Environment.NewLine, lines.Skip(1)) : "Esito aggiornamento non disponibile.";
            if (lines.FirstOrDefault() == "SUCCESS")
                MessageBox.Show(message, "Aggiornamento completato", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show(message, "Aggiornamento non riuscito", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Dispose();
        base.OnExit(e);
    }

    public void Dispose()
    {
        mainViewModel?.Dispose();
        mainViewModel = null;
        trayIconService?.Dispose();
        trayIconService = null;
        themeService?.Dispose();
        themeService = null;
        GC.SuppressFinalize(this);
    }
}
