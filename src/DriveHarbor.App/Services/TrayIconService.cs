using System.ComponentModel;
using System.Drawing;
using System.Windows;
using DriveHarbor.App.ViewModels;
using Forms = System.Windows.Forms;

namespace DriveHarbor.App.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly MainWindow window;
    private readonly MainViewModel viewModel;
    private readonly Forms.NotifyIcon notifyIcon;
    private readonly Forms.ToolStripMenuItem synchronizeItem;
    private readonly Icon applicationIcon;

    public TrayIconService(MainWindow window, MainViewModel viewModel)
    {
        this.window = window;
        this.viewModel = viewModel;
        applicationIcon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!)
            ?? SystemIcons.Application;

        var logo = new Bitmap(applicationIcon.ToBitmap(), new System.Drawing.Size(16, 16));
        var openItem = new Forms.ToolStripMenuItem("Apri DriveHarbor", logo, (_, _) => ShowWindow());
        synchronizeItem = new Forms.ToolStripMenuItem("Sincronizza ora", null, (_, _) => Synchronize());
        var exitItem = new Forms.ToolStripMenuItem("Esci", null, (_, _) => window.ExitApplication());
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(openItem);
        menu.Items.Add(synchronizeItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(exitItem);

        notifyIcon = new()
        {
            Text = "DriveHarbor",
            Icon = applicationIcon,
            ContextMenuStrip = menu,
        };
        notifyIcon.DoubleClick += (_, _) => ShowWindow();
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        UpdateVisibility();
    }

    public void ShowBackgroundNotification()
    {
        notifyIcon.Visible = true;
        notifyIcon.ShowBalloonTip(
            4000,
            "DriveHarbor è ancora attivo",
            "La sincronizzazione continua in background. Usa l'icona vicino all'orologio per riaprire o uscire.",
            Forms.ToolTipIcon.Info);
    }

    public void Dispose()
    {
        viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        notifyIcon.Visible = false;
        notifyIcon.Dispose();
        applicationIcon.Dispose();
    }

    private void ShowWindow()
    {
        window.Show();
        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }
        window.Activate();
    }

    private void Synchronize()
    {
        if (viewModel.SynchronizeCommand.CanExecute(null))
        {
            viewModel.SynchronizeCommand.Execute(null);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.KeepRunningInBackground))
        {
            UpdateVisibility();
        }
        else if (e.PropertyName is nameof(MainViewModel.IsBusy)
            or nameof(MainViewModel.IsSsdAvailable)
            or nameof(MainViewModel.IsOneDriveAvailable))
        {
            synchronizeItem.Enabled = viewModel.SynchronizeCommand.CanExecute(null);
        }
    }

    private void UpdateVisibility()
    {
        notifyIcon.Visible = viewModel.KeepRunningInBackground;
        synchronizeItem.Enabled = viewModel.SynchronizeCommand.CanExecute(null);
    }
}
