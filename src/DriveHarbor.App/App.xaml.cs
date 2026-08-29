using System.Windows;
using DriveHarbor.App.Services;
using DriveHarbor.App.ViewModels;
using DriveHarbor.Core.Configuration;
using DriveHarbor.Core.Drives;
using DriveHarbor.Core.Robocopy;
using DriveHarbor.Core.Validation;

namespace DriveHarbor.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application, IDisposable
{
    private MainViewModel? mainViewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var driveDetection = new DriveDetectionService(new WindowsVolumeCatalog());
        var pathValidator = new PathSafetyValidator(new EnvironmentOneDriveRootProvider());
        mainViewModel = new(
            new JsonConfigurationStore(),
            driveDetection,
            pathValidator,
            new RobocopyRunner(),
            new FolderPicker(),
            new UserDialog());

        var window = new MainWindow(mainViewModel);
        MainWindow = window;
        window.Show();
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
        GC.SuppressFinalize(this);
    }
}
