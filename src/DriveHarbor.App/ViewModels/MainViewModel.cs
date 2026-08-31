using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Windows;
using DriveHarbor.App.Infrastructure;
using DriveHarbor.App.Services;
using DriveHarbor.Core.Configuration;
using DriveHarbor.Core.Drives;
using DriveHarbor.Core.Logging;
using DriveHarbor.Core.Robocopy;
using DriveHarbor.Core.Synchronization;
using DriveHarbor.Core.Updates;
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
    private readonly IThemeService themeService;
    private readonly IUpdateChecker updateChecker;
    private readonly IUpdateDownloader updateDownloader;
    private readonly IUpdateInstaller updateInstaller;
    private AppSettings savedSettings = AppSettings.CreateDefault();
    private CancellationTokenSource? synchronizationCancellation;
    private CancellationTokenSource? autoSyncDelayCancellation;
    private DriveConnectionStatus? previousDriveStatus;
    private string? sourcePath;
    private string? destinationPath;
    private SyncMode mode;
    private AppTheme theme = AppTheme.System;
    private UpdateChannel updateChannel = UpdateChannel.Stable;
    private Uri? updateUri;
    private UpdateCheckResult? lastAvailableUpdate;
    private string updateMessage = string.Empty;
    private string exclusionsText = string.Empty;
    private string logDirectory = AppPaths.DefaultLogDirectory;
    private string ssdStatus = "Non configurato";
    private string oneDriveStatus = "Non configurato";
    private string operationStatus = "Configura le cartelle per iniziare";
    private string lastSynchronization = "Mai";
    private string lastResult = "Nessuna sincronizzazione eseguita";
    private bool isBusy;
    private bool isSettingsPageVisible;
    private bool syncOnDriveConnected;
    private bool allowAutomaticMirror;
    private int driveConnectedDelaySeconds = 10;

    public MainViewModel(
        IConfigurationStore configurationStore,
        DriveDetectionService driveDetectionService,
        PathSafetyValidator pathSafetyValidator,
        IRobocopyRunner robocopyRunner,
        IFolderPicker folderPicker,
        IUserDialog userDialog,
        IThemeService themeService,
        IUpdateChecker updateChecker,
        IUpdateDownloader updateDownloader,
        IUpdateInstaller updateInstaller)
    {
        this.configurationStore = configurationStore;
        this.driveDetectionService = driveDetectionService;
        this.pathSafetyValidator = pathSafetyValidator;
        this.robocopyRunner = robocopyRunner;
        this.folderPicker = folderPicker;
        this.userDialog = userDialog;
        this.themeService = themeService;
        this.updateChecker = updateChecker;
        this.updateDownloader = updateDownloader;
        this.updateInstaller = updateInstaller;

        ShowDashboardCommand = new(() => IsSettingsPageVisible = false);
        ShowSettingsCommand = new(() => IsSettingsPageVisible = true);
        CancelSettingsCommand = new(CancelSettingsChanges);
        BrowseSourceCommand = new(BrowseSource);
        BrowseDestinationCommand = new(BrowseDestination);
        BrowseLogDirectoryCommand = new(BrowseLogDirectory);
        SaveSettingsCommand = new(SaveSettingsAsync, () => !IsBusy, HandleUnexpectedError);
        SynchronizeCommand = new(SynchronizeAsync, CanSynchronize, HandleUnexpectedError);
        CancelCommand = new(CancelSynchronization, () => IsBusy || autoSyncDelayCancellation is not null);
        CheckForUpdatesCommand = new(() => CheckForUpdatesAsync(true), () => !IsBusy, HandleUnexpectedError);
        OpenUpdateCommand = new(OpenUpdate, () => updateUri is not null);
        DownloadUpdateCommand = new(DownloadUpdateAsync, () => updateUri is not null && !IsBusy, HandleUnexpectedError);
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

    public AsyncRelayCommand CheckForUpdatesCommand { get; }

    public RelayCommand OpenUpdateCommand { get; }

    public AsyncRelayCommand DownloadUpdateCommand { get; }

    public ObservableCollection<string> LogLines { get; } = [];

    public IReadOnlyList<SyncMode> AvailableModes { get; } = Enum.GetValues<SyncMode>();

    public string ApplicationVersion { get; } = GetApplicationVersion();

    public IReadOnlyList<UpdateChannel> AvailableUpdateChannels { get; } = Enum.GetValues<UpdateChannel>();

    public UpdateChannel UpdateChannel
    {
        get => updateChannel;
        set => SetProperty(ref updateChannel, value);
    }

    public string UpdateMessage
    {
        get => updateMessage;
        private set => SetProperty(ref updateMessage, value);
    }

    public Visibility UpdateVisibility => updateUri is null ? Visibility.Collapsed : Visibility.Visible;

    public bool SyncOnDriveConnected
    {
        get => syncOnDriveConnected;
        set => SetProperty(ref syncOnDriveConnected, value);
    }

    public bool AllowAutomaticMirror
    {
        get => allowAutomaticMirror;
        set => SetProperty(ref allowAutomaticMirror, value);
    }

    public IReadOnlyList<int> AvailableDriveConnectedDelays { get; } = [5, 10, 30, 60, 120, 300, 600];

    public int DriveConnectedDelaySeconds
    {
        get => driveConnectedDelaySeconds;
        set => SetProperty(ref driveConnectedDelaySeconds, value);
    }

    public bool UseSystemTheme
    {
        get => Theme == AppTheme.System;
        set { if (value) Theme = AppTheme.System; }
    }

    public bool UseLightTheme
    {
        get => Theme == AppTheme.Light;
        set { if (value) Theme = AppTheme.Light; }
    }

    public bool UseDarkTheme
    {
        get => Theme == AppTheme.Dark;
        set { if (value) Theme = AppTheme.Dark; }
    }

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

    public AppTheme Theme
    {
        get => theme;
        set
        {
            if (SetProperty(ref theme, value))
            {
                themeService.Apply(value);
                OnPropertyChanged(nameof(UseSystemTheme));
                OnPropertyChanged(nameof(UseLightTheme));
                OnPropertyChanged(nameof(UseDarkTheme));
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

        if (savedSettings.LastUpdateCheckUtc is null
            || DateTimeOffset.UtcNow - savedSettings.LastUpdateCheckUtc >= TimeSpan.FromHours(24))
        {
            await CheckForUpdatesAsync(false);
        }
    }

    public void RefreshAvailability()
    {
        var drive = driveDetectionService.Resolve(savedSettings.SourcePath, savedSettings.SourceDrive);
        var isFirstObservation = previousDriveStatus is null;
        var wasConnected = previousDriveStatus == DriveConnectionStatus.Connected;
        previousDriveStatus = drive.Status;
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
        if (drive.Status != DriveConnectionStatus.Connected)
        {
            CancelPendingAutoSync();
        }
        else if (!isFirstObservation && !wasConnected && savedSettings.SyncOnDriveConnected)
        {
            _ = ScheduleAutoSyncAsync();
        }
        NotifyCommandStates();
    }

    public void Dispose()
    {
        synchronizationCancellation?.Cancel();
        synchronizationCancellation?.Dispose();
        CancelPendingAutoSync();
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

        if (Mode == SyncMode.Mirror && SyncOnDriveConnected && AllowAutomaticMirror
            && !savedSettings.AllowAutomaticMirror
            && !userDialog.Confirm(
                "Consentire Mirror automatico?",
                "Mirror automatico può eliminare dalla destinazione senza conferma a ogni collegamento. L'anteprima tecnica verrà comunque eseguita. Vuoi abilitarlo?"))
        {
            AllowAutomaticMirror = false;
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

    private Task SynchronizeAsync() => SynchronizeCoreAsync(automatic: false);

    private async Task SynchronizeCoreAsync(bool automatic)
    {
        IsBusy = true;
        LogLines.Clear();
        synchronizationCancellation = new();

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
                if (!automatic && !userDialog.Confirm(
                    "Analizzare Mirror?",
                    "DriveHarbor analizzerà le modifiche senza cambiare alcun file. Continuare?"))
                {
                    return;
                }

                OperationStatus = "Analisi Mirror in corso";
                AppendLogLine("Anteprima Mirror in corso…");
                var preview = await service.PreviewMirrorAsync(
                    savedSettings,
                    progress: null,
                    synchronizationCancellation.Token);
                ApplySynchronizationResult(preview, persistHistory: false);
                AppendFriendlySummary(preview, isPreview: true);
                if (preview.Status is not SynchronizationStatus.Completed
                    and not SynchronizationStatus.CompletedWithWarnings)
                {
                    return;
                }

                if (!automatic && !userDialog.Confirm(
                    "Conferma Mirror",
                    "L'anteprima è completata. Mirror può eliminare file soltanto dalla destinazione. Vuoi eseguire ora la sincronizzazione?"))
                {
                    OperationStatus = "Mirror non eseguito";
                    return;
                }
            }

            OperationStatus = "Sincronizzazione in corso";
            AppendLogLine(savedSettings.Mode == SyncMode.Mirror
                ? "Sincronizzazione Mirror in corso…"
                : "Backup in corso…");
            var result = await service.SynchronizeAsync(
                savedSettings,
                mirrorConfirmed: savedSettings.Mode == SyncMode.Mirror,
                progress: null,
                synchronizationCancellation.Token);
            ApplySynchronizationResult(result, persistHistory: true);
            AppendFriendlySummary(result, isPreview: false);
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

    private void CancelSynchronization()
    {
        synchronizationCancellation?.Cancel();
        CancelPendingAutoSync();
    }

    private async Task ScheduleAutoSyncAsync()
    {
        if (autoSyncDelayCancellation is not null || IsBusy) return;
        if (savedSettings.Mode == SyncMode.Mirror && !savedSettings.AllowAutomaticMirror)
        {
            OperationStatus = "Avvio automatico bloccato: Mirror automatico non autorizzato";
            return;
        }

        autoSyncDelayCancellation = new();
        NotifyCommandStates();
        var token = autoSyncDelayCancellation.Token;
        try
        {
            for (var remaining = savedSettings.DriveConnectedDelaySeconds; remaining > 0; remaining--)
            {
                OperationStatus = $"Sincronizzazione automatica tra {remaining} secondi";
                await Task.Delay(TimeSpan.FromSeconds(1), token);
            }
            var drive = driveDetectionService.Resolve(savedSettings.SourcePath, savedSettings.SourceDrive);
            if (drive.Status == DriveConnectionStatus.Connected && !IsBusy)
            {
                await SynchronizeCoreAsync(automatic: true);
            }
        }
        catch (OperationCanceledException)
        {
            OperationStatus = "Sincronizzazione automatica annullata";
        }
        catch (Exception exception)
        {
            HandleUnexpectedError(exception);
        }
        finally
        {
            autoSyncDelayCancellation?.Dispose();
            autoSyncDelayCancellation = null;
            NotifyCommandStates();
        }
    }

    private void CancelPendingAutoSync() => autoSyncDelayCancellation?.Cancel();

    private bool CanSynchronize() =>
        !IsBusy
        && savedSettings.SourceDrive is not null
        && !string.IsNullOrWhiteSpace(savedSettings.SourcePath)
        && !string.IsNullOrWhiteSpace(savedSettings.DestinationPath);

    private void ApplySettings(AppSettings settings)
    {
        themeService.Apply(settings.Theme);
        Theme = settings.Theme;
        UpdateChannel = settings.UpdateChannel;
        SyncOnDriveConnected = settings.SyncOnDriveConnected;
        AllowAutomaticMirror = settings.AllowAutomaticMirror;
        DriveConnectedDelaySeconds = settings.DriveConnectedDelaySeconds;
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
        Theme = Theme,
        UpdateChannel = UpdateChannel,
        SyncOnDriveConnected = SyncOnDriveConnected,
        AllowAutomaticMirror = AllowAutomaticMirror,
        DriveConnectedDelaySeconds = DriveConnectedDelaySeconds,
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

    private void AppendFriendlySummary(SynchronizationResult result, bool isPreview)
    {
        AppendLogLine(string.Empty);
        AppendLogLine(isPreview ? "RIEPILOGO ANTEPRIMA" : "RIEPILOGO FINALE");
        AppendLogLine(result.UserMessage);

        if (result.Summary is not { } summary || summary.TotalFiles is null)
        {
            AppendLogLine("I conteggi dei file non sono disponibili.");
            return;
        }

        AppendLogLine($"File esaminati: {summary.TotalFiles.Value:N0}");
        AppendLogLine($"File {(isPreview ? "da aggiungere o aggiornare" : "aggiunti o aggiornati")}: {summary.CopiedFiles ?? 0:N0}");
        AppendLogLine($"File già sincronizzati: {summary.SkippedFiles ?? 0:N0}");

        if (savedSettings.Mode == SyncMode.Mirror)
        {
            AppendLogLine($"File {(isPreview ? "da eliminare" : "eliminati")}: {summary.ExtraFiles ?? 0:N0}");
        }
        else if (summary.ExtraFiles > 0)
        {
            AppendLogLine($"File presenti solo nella destinazione (mantenuti): {summary.ExtraFiles.Value:N0}");
        }

        if (summary.MismatchedFiles > 0)
        {
            AppendLogLine($"File non corrispondenti: {summary.MismatchedFiles.Value:N0}");
        }

        AppendLogLine($"Errori sui file: {summary.FailedFiles ?? 0:N0}");
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
        CheckForUpdatesCommand.NotifyCanExecuteChanged();
        DownloadUpdateCommand.NotifyCanExecuteChanged();
    }

    private void HandleUnexpectedError(Exception exception)
    {
        OperationStatus = "Errore";
        userDialog.ShowError(
            "Errore imprevisto",
            "L'operazione è stata interrotta senza modificare la sorgente. " + exception.Message);
    }

    private static string GetApplicationVersion()
    {
        var informationalVersion = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        var version = informationalVersion?.Split('+', 2)[0];
        return $"Versione {version ?? "non disponibile"}";
    }

    private async Task CheckForUpdatesAsync(bool showCurrentMessage)
    {
        try
        {
            var result = await updateChecker.CheckAsync(GetRawApplicationVersion(), UpdateChannel);
            savedSettings = savedSettings with { LastUpdateCheckUtc = DateTimeOffset.UtcNow };
            await configurationStore.SaveAsync(savedSettings);
            if (result.IsAvailable)
            {
                updateUri = result.ReleaseUri;
                lastAvailableUpdate = result;
                UpdateMessage = $"È disponibile DriveHarbor {result.Version}.";
                OnPropertyChanged(nameof(UpdateVisibility));
                OpenUpdateCommand.NotifyCanExecuteChanged();
                DownloadUpdateCommand.NotifyCanExecuteChanged();
            }
            else if (showCurrentMessage)
            {
                userDialog.ShowInformation("Aggiornamenti", "Stai usando la versione più recente del canale selezionato.");
            }
        }
        catch (HttpRequestException) when (!showCurrentMessage)
        {
        }
        catch (HttpRequestException exception)
        {
            userDialog.ShowError("Aggiornamenti", $"Impossibile controllare gli aggiornamenti. {exception.Message}");
        }
    }

    private void OpenUpdate()
    {
        if (updateUri is { Scheme: "https", Host: "github.com" })
        {
            Process.Start(new ProcessStartInfo(updateUri.AbsoluteUri) { UseShellExecute = true });
        }
    }

    private async Task DownloadUpdateAsync()
    {
        var result = await updateDownloader.DownloadAsync(lastAvailableUpdate!);
        if (result.Succeeded)
        {
            if (userDialog.Confirm("Installare l'aggiornamento?", $"{result.UserMessage}\n\nDriveHarbor verrà chiuso, aggiornato e riaperto. Continuare?"))
            {
                if (updateInstaller.TryStart(result.PackagePath!, out var errorMessage))
                {
                    Application.Current.Shutdown();
                }
                else
                {
                    userDialog.ShowError("Aggiornamento", errorMessage!);
                }
            }
        }
        else
        {
            userDialog.ShowError("Aggiornamento rifiutato", result.UserMessage);
        }
    }

    private static string GetRawApplicationVersion() => Assembly.GetEntryAssembly()?
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion.Split('+', 2)[0] ?? "0.0.0";
}
