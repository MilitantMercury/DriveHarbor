using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using DriveHarbor.App.Infrastructure;
using DriveHarbor.App.Services;
using DriveHarbor.Core.Configuration;
using DriveHarbor.Core.Drives;
using DriveHarbor.Core.Logging;
using DriveHarbor.Core.Robocopy;
using DriveHarbor.Core.Synchronization;
using DriveHarbor.Core.Validation;

namespace DriveHarbor.App.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private readonly IConfigurationStore configurationStore;
    private readonly DriveDetectionService driveDetectionService;
    private readonly PathSafetyValidator pathSafetyValidator;
    private readonly IRobocopyRunner robocopyRunner;
    private readonly IFolderPicker folderPicker;
    private readonly IUserDialog userDialog;
    private AppSettings savedSettings = AppSettings.CreateDefault();
    private CancellationTokenSource? synchronizationCancellation;
    private string? sourcePath;
    private string? destinationPath;
    private SyncMode mode;
    private string exclusionsText = string.Empty;
    private string logDirectory = AppPaths.DefaultLogDirectory;
    private string ssdStatus = "Non configurato";
    private string oneDriveStatus = "Non configurato";
    private string operationStatus = "Configura le cartelle per iniziare";
    private string lastSynchronization = "Mai";
    private string lastResult = "Nessuna sincronizzazione eseguita";
    private bool isBusy;
    private bool isSettingsPageVisible;

    public MainViewModel(
        IConfigurationStore configurationStore,
        DriveDetectionService driveDetectionService,
        PathSafetyValidator pathSafetyValidator,
        IRobocopyRunner robocopyRunner,
        IFolderPicker folderPicker,
        IUserDialog userDialog)
    {
        this.configurationStore = configurationStore;
        this.driveDetectionService = driveDetectionService;
        this.pathSafetyValidator = pathSafetyValidator;
        this.robocopyRunner = robocopyRunner;
        this.folderPicker = folderPicker;
        this.userDialog = userDialog;

        ShowDashboardCommand = new(() => IsSettingsPageVisible = false);
        ShowSettingsCommand = new(() => IsSettingsPageVisible = true);
        CancelSettingsCommand = new(CancelSettingsChanges);
        BrowseSourceCommand = new(BrowseSource);
        BrowseDestinationCommand = new(BrowseDestination);
        BrowseLogDirectoryCommand = new(BrowseLogDirectory);
        SaveSettingsCommand = new(SaveSettingsAsync, () => !IsBusy, HandleUnexpectedError);
        SynchronizeCommand = new(SynchronizeAsync, CanSynchronize, HandleUnexpectedError);
        CancelCommand = new(CancelSynchronization, () => IsBusy);
    }

    public RelayCommand ShowDashboardCommand { get; }

    public RelayCommand ShowSettingsCommand { get; }

    public RelayCommand CancelSettingsCommand { get; }

    public RelayCommand BrowseSourceCommand { get; }

    public RelayCommand BrowseDestinationCommand { get; }

    public RelayCommand BrowseLogDirectoryCommand { get; }

    public AsyncRelayCommand SaveSettingsCommand { get; }

    public AsyncRelayCommand SynchronizeCommand { get; }

    public RelayCommand CancelCommand { get; }

    public ObservableCollection<string> LogLines { get; } = [];

    public IReadOnlyList<SyncMode> AvailableModes { get; } = Enum.GetValues<SyncMode>();

    public string? SourcePath
    {
        get => sourcePath;
        set => SetProperty(ref sourcePath, value);
    }

    public string? DestinationPath
    {
        get => destinationPath;
        set => SetProperty(ref destinationPath, value);
    }

    public SyncMode Mode
    {
        get => mode;
        set
        {
            if (SetProperty(ref mode, value))
            {
                OnPropertyChanged(nameof(ModeSummary));
            }
        }
    }

    public string ModeSummary => Mode == SyncMode.Backup
        ? "Copia e aggiorna senza eliminare dalla destinazione"
        : "Rende la destinazione identica alla sorgente e può eliminare file";

    public string ExclusionsText
    {
        get => exclusionsText;
        set => SetProperty(ref exclusionsText, value);
    }

    public string LogDirectory
    {
        get => logDirectory;
        set => SetProperty(ref logDirectory, value);
    }

    public string SsdStatus
    {
        get => ssdStatus;
        private set => SetProperty(ref ssdStatus, value);
    }

    public string OneDriveStatus
    {
        get => oneDriveStatus;
        private set => SetProperty(ref oneDriveStatus, value);
    }

    public string OperationStatus
    {
        get => operationStatus;
        private set => SetProperty(ref operationStatus, value);
    }

    public string LastSynchronization
    {
        get => lastSynchronization;
        private set => SetProperty(ref lastSynchronization, value);
    }

    public string LastResult
    {
        get => lastResult;
        private set => SetProperty(ref lastResult, value);
    }

    public bool IsBusy
    {
        get => isBusy;
        private set
        {
            if (SetProperty(ref isBusy, value))
            {
                NotifyCommandStates();
            }
        }
    }

    public bool IsSettingsPageVisible
    {
        get => isSettingsPageVisible;
        private set
        {
            if (SetProperty(ref isSettingsPageVisible, value))
            {
                OnPropertyChanged(nameof(DashboardVisibility));
                OnPropertyChanged(nameof(SettingsVisibility));
            }
        }
    }

    public Visibility DashboardVisibility =>
        IsSettingsPageVisible ? Visibility.Collapsed : Visibility.Visible;

    public Visibility SettingsVisibility =>
        IsSettingsPageVisible ? Visibility.Visible : Visibility.Collapsed;

    public async Task InitializeAsync()
    {
        var loadResult = await configurationStore.LoadAsync();
        savedSettings = loadResult.Settings;
        ApplySettings(savedSettings);
        RefreshAvailability();

        if (!string.IsNullOrWhiteSpace(loadResult.UserMessage))
        {
            userDialog.ShowInformation("Configurazione", loadResult.UserMessage);
        }
    }

    public void RefreshAvailability()
    {
        var drive = driveDetectionService.Resolve(savedSettings.SourcePath, savedSettings.SourceDrive);
        SsdStatus = drive.Status switch
        {
            DriveConnectionStatus.Connected => "SSD collegato",
            DriveConnectionStatus.Disconnected => "SSD non collegato",
            DriveConnectionStatus.Ambiguous => "Identità SSD ambigua",
            DriveConnectionStatus.SourceFolderUnavailable => "Cartella sorgente non disponibile",
            _ => "SSD non configurato",
        };

        OneDriveStatus = !string.IsNullOrWhiteSpace(savedSettings.DestinationPath)
            && Directory.Exists(savedSettings.DestinationPath)
                ? "Cartella disponibile"
                : "Destinazione non disponibile";
        NotifyCommandStates();
    }

    public void Dispose()
    {
        synchronizationCancellation?.Cancel();
        synchronizationCancellation?.Dispose();
    }

    private void BrowseSource()
    {
        var selected = folderPicker.PickFolder("Seleziona la cartella sul tuo SSD", SourcePath);
        if (selected is not null)
        {
            SourcePath = selected;
        }
    }

    private void BrowseDestination()
    {
        var selected = folderPicker.PickFolder("Seleziona una cartella dentro OneDrive", DestinationPath);
        if (selected is not null)
        {
            DestinationPath = selected;
        }
    }

    private void BrowseLogDirectory()
    {
        var selected = folderPicker.PickFolder("Seleziona la cartella dei log", LogDirectory);
        if (selected is not null)
        {
            LogDirectory = selected;
        }
    }

    private async Task SaveSettingsAsync()
    {
        var validation = pathSafetyValidator.Validate(SourcePath, DestinationPath);
        if (!validation.IsValid)
        {
            userDialog.ShowError("Impostazioni non valide", validation.Issues[0].UserMessage);
            return;
        }

        var capture = driveDetectionService.Capture(SourcePath);
        if (capture.Status != DriveCaptureStatus.Captured)
        {
            userDialog.ShowError("SSD non riconosciuto", capture.UserMessage ?? "Impossibile identificare il volume.");
            return;
        }

        var logValidation = LogDirectorySafetyValidator.Validate(
            LogDirectory,
            SourcePath,
            DestinationPath);
        if (!logValidation.IsValid)
        {
            userDialog.ShowError("Posizione log non valida", logValidation.UserMessage!);
            return;
        }

        if (Mode == SyncMode.Mirror
            && savedSettings.Mode != SyncMode.Mirror
            && !userDialog.Confirm(
                "Attivare Mirror?",
                "Mirror può eliminare dalla destinazione i file non più presenti sulla sorgente. La sorgente non verrà mai modificata. Vuoi attivarlo?"))
        {
            Mode = SyncMode.Backup;
            return;
        }

        var updated = BuildEditedSettings() with { SourceDrive = capture.Fingerprint };
        await configurationStore.SaveAsync(updated);
        savedSettings = updated;
        ApplySettings(updated);
        RefreshAvailability();
        IsSettingsPageVisible = false;
        OperationStatus = "Pronto";
        userDialog.ShowInformation("Impostazioni", "Impostazioni salvate.");
    }

    private async Task SynchronizeAsync()
    {
        IsBusy = true;
        LogLines.Clear();
        synchronizationCancellation = new();
        var progress = new Progress<string>(AppendLogLine);

        try
        {
            using var logger = new DailyFileLogger(new()
            {
                DirectoryPath = savedSettings.LogDirectory,
            });
            var service = new SynchronizationService(
                driveDetectionService,
                pathSafetyValidator,
                robocopyRunner,
                logger);

            if (savedSettings.Mode == SyncMode.Mirror)
            {
                if (!userDialog.Confirm(
                    "Analizzare Mirror?",
                    "DriveHarbor analizzerà le modifiche senza cambiare alcun file. Continuare?"))
                {
                    return;
                }

                OperationStatus = "Analisi Mirror in corso";
                var preview = await service.PreviewMirrorAsync(
                    savedSettings,
                    progress,
                    synchronizationCancellation.Token);
                ApplySynchronizationResult(preview, persistHistory: false);
                if (preview.Status is not SynchronizationStatus.Completed
                    and not SynchronizationStatus.CompletedWithWarnings)
                {
                    return;
                }

                if (!userDialog.Confirm(
                    "Conferma Mirror",
                    "L'anteprima è completata. Mirror può eliminare file soltanto dalla destinazione. Vuoi eseguire ora la sincronizzazione?"))
                {
                    OperationStatus = "Mirror non eseguito";
                    return;
                }
            }

            OperationStatus = "Sincronizzazione in corso";
            var result = await service.SynchronizeAsync(
                savedSettings,
                mirrorConfirmed: savedSettings.Mode == SyncMode.Mirror,
                progress,
                synchronizationCancellation.Token);
            ApplySynchronizationResult(result, persistHistory: true);
            await PersistHistoryAsync(result);
        }
        finally
        {
            synchronizationCancellation.Dispose();
            synchronizationCancellation = null;
            IsBusy = false;
            RefreshAvailability();
        }
    }

    private void CancelSynchronization() => synchronizationCancellation?.Cancel();

    private bool CanSynchronize() =>
        !IsBusy
        && savedSettings.SourceDrive is not null
        && !string.IsNullOrWhiteSpace(savedSettings.SourcePath)
        && !string.IsNullOrWhiteSpace(savedSettings.DestinationPath);

    private void ApplySettings(AppSettings settings)
    {
        SourcePath = settings.SourcePath;
        DestinationPath = settings.DestinationPath;
        Mode = settings.Mode;
        ExclusionsText = string.Join(Environment.NewLine, settings.Exclusions);
        LogDirectory = settings.LogDirectory;
        LastSynchronization = settings.LastSynchronizationUtc?.ToLocalTime()
            .ToString("g", CultureInfo.CurrentCulture) ?? "Mai";
        LastResult = settings.LastSynchronizationResult is { } result
            ? settings.LastCopiedFiles is { } copied
                ? $"{result} File aggiornati: {copied}."
                : result
            : "Nessuna sincronizzazione eseguita";
    }

    private AppSettings BuildEditedSettings() => savedSettings with
    {
        SourcePath = SourcePath,
        DestinationPath = DestinationPath,
        Mode = Mode,
        Exclusions = ExclusionsText
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray(),
        LogDirectory = LogDirectory,
    };

    private async Task PersistHistoryAsync(SynchronizationResult result)
    {
        var timestamp = DateTimeOffset.UtcNow;
        savedSettings = savedSettings with
        {
            LastSynchronizationUtc = timestamp,
            LastSynchronizationResult = result.UserMessage,
            LastCopiedFiles = result.Summary?.CopiedFiles,
        };
        await configurationStore.SaveAsync(savedSettings);
        LastSynchronization = timestamp.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
        LastResult = result.Summary?.CopiedFiles is { } copied
            ? $"{result.UserMessage} File aggiornati: {copied}."
            : result.UserMessage;
    }

    private void ApplySynchronizationResult(SynchronizationResult result, bool persistHistory)
    {
        OperationStatus = result.Status switch
        {
            SynchronizationStatus.Completed => "Completato",
            SynchronizationStatus.CompletedWithWarnings => "Completato con avvisi",
            SynchronizationStatus.Cancelled => "Annullato",
            SynchronizationStatus.SsdNotConnected => "SSD non collegato",
            SynchronizationStatus.DestinationUnavailable => "Destinazione non disponibile",
            _ => "Errore",
        };
        LastResult = result.UserMessage;
        if (persistHistory && result.Summary?.CopiedFiles is { } copied)
        {
            LastResult = $"{result.UserMessage} File aggiornati: {copied}.";
        }
    }

    private void AppendLogLine(string line)
    {
        if (LogLines.Count == 500)
        {
            LogLines.RemoveAt(0);
        }

        LogLines.Add(line);
    }

    private void CancelSettingsChanges()
    {
        ApplySettings(savedSettings);
        IsSettingsPageVisible = false;
    }

    private void NotifyCommandStates()
    {
        SaveSettingsCommand.NotifyCanExecuteChanged();
        SynchronizeCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
    }

    private void HandleUnexpectedError(Exception exception)
    {
        OperationStatus = "Errore";
        userDialog.ShowError(
            "Errore imprevisto",
            "L'operazione è stata interrotta senza modificare la sorgente. " + exception.Message);
    }
}
