using System.Windows;
using System.Windows.Threading;
using DriveHarbor.App.ViewModels;

namespace DriveHarbor.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel viewModel;
    private readonly DispatcherTimer availabilityTimer;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        DataContext = viewModel;
        availabilityTimer = new()
        {
            Interval = TimeSpan.FromSeconds(5),
        };
        availabilityTimer.Tick += (_, _) => viewModel.RefreshAvailability();
        Loaded += OnLoaded;
        Closed += (_, _) => availabilityTimer.Stop();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await viewModel.InitializeAsync();
        availabilityTimer.Start();
    }
}
