using System.Windows;
using System.ComponentModel;
using System.Windows.Threading;
using DriveHarbor.App.Services;
using DriveHarbor.App.ViewModels;

namespace DriveHarbor.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel viewModel;
    private readonly DispatcherTimer availabilityTimer;
    private readonly bool startedFromWindows;
    private TrayIconService? trayIconService;
    private bool allowClose;
    private bool backgroundNotificationShown;

    public MainWindow(MainViewModel viewModel, bool startedFromWindows = false)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        this.startedFromWindows = startedFromWindows;
        DataContext = viewModel;
        availabilityTimer = new()
        {
            Interval = TimeSpan.FromSeconds(5),
        };
        availabilityTimer.Tick += (_, _) => viewModel.RefreshAvailability();
        Loaded += OnLoaded;
        Closing += OnClosing;
        System.Windows.Application.Current.SessionEnding += OnSessionEnding;
        Closed += (_, _) =>
        {
            availabilityTimer.Stop();
            System.Windows.Application.Current.SessionEnding -= OnSessionEnding;
        };
    }

    public void AttachTrayIcon(TrayIconService service) => trayIconService = service;

    public void ExitApplication()
    {
        allowClose = true;
        Close();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await viewModel.InitializeAsync();
        availabilityTimer.Start();
        if (startedFromWindows && viewModel.StartMinimizedToTray && viewModel.KeepRunningInBackground)
        {
            Hide();
        }
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (allowClose || !viewModel.KeepRunningInBackground)
        {
            return;
        }

        e.Cancel = true;
        Hide();
        if (!backgroundNotificationShown)
        {
            trayIconService?.ShowBackgroundNotification();
            backgroundNotificationShown = true;
        }
    }

    private void OnSessionEnding(object? sender, SessionEndingCancelEventArgs e)
    {
        allowClose = true;
    }
}
