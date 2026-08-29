using DriveHarbor.Core.Configuration;
using DriveHarbor.Core.Drives;
using DriveHarbor.Core.Logging;
using DriveHarbor.Core.Robocopy;
using DriveHarbor.Core.Validation;

namespace DriveHarbor.Core.Synchronization;

public sealed class SynchronizationService
{
    private const long MinimumFreeBytes = 256L * 1024 * 1024;
    private readonly DriveDetectionService driveDetectionService;
    private readonly PathSafetyValidator pathSafetyValidator;
    private readonly IRobocopyRunner robocopyRunner;
    private readonly IAppLogger logger;
    private readonly IDiskSpaceProvider diskSpaceProvider;

    public SynchronizationService(
        DriveDetectionService driveDetectionService,
        PathSafetyValidator pathSafetyValidator,
        IRobocopyRunner robocopyRunner,
        IAppLogger logger,
        IDiskSpaceProvider? diskSpaceProvider = null)
    {
        this.driveDetectionService = driveDetectionService;
        this.pathSafetyValidator = pathSafetyValidator;
        this.robocopyRunner = robocopyRunner;
        this.logger = logger;
        this.diskSpaceProvider = diskSpaceProvider ?? new WindowsDiskSpaceProvider();
    }

    public async Task<SynchronizationResult> PreviewMirrorAsync(
        AppSettings settings,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.Mode != SyncMode.Mirror)
        {
            return new(
                SynchronizationStatus.InvalidConfiguration,
                "L'anteprima è disponibile soltanto per la modalità Mirror.");
        }

        return await RunCoreAsync(
            settings,
            dryRun: true,
            mirrorConfirmed: false,
            progress,
            cancellationToken).ConfigureAwait(false);
    }

    public Task<SynchronizationResult> SynchronizeAsync(
        AppSettings settings,
        bool mirrorConfirmed,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.Mode == SyncMode.Mirror && !mirrorConfirmed)
        {
            return Task.FromResult(new SynchronizationResult(
                SynchronizationStatus.MirrorConfirmationRequired,
                "Conferma esplicitamente la modalità Mirror prima di continuare."));
        }

        return RunCoreAsync(
            settings,
            dryRun: false,
            mirrorConfirmed,
            progress,
            cancellationToken);
    }

    private async Task<SynchronizationResult> RunCoreAsync(
        AppSettings settings,
        bool dryRun,
        bool mirrorConfirmed,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var preflight = Preflight(settings);
        if (preflight.Failure is not null)
        {
            await logger.WriteAsync(LogLevel.Warning, preflight.Failure.UserMessage, cancellationToken)
                .ConfigureAwait(false);
            return preflight.Failure;
        }

        await logger.WriteAsync(
            LogLevel.Information,
            dryRun
                ? "Avvio anteprima Mirror."
                : $"Avvio sincronizzazione in modalità {settings.Mode}.",
            cancellationToken).ConfigureAwait(false);

        var result = await robocopyRunner.RunAsync(
            new(
                preflight.ResolvedSourcePath!,
                settings.DestinationPath!,
                settings.Mode,
                settings.Exclusions,
                dryRun,
                mirrorConfirmed),
            progress,
            cancellationToken).ConfigureAwait(false);

        await PersistProcessLogAsync(result, CancellationToken.None).ConfigureAwait(false);
        return MapResult(result, dryRun);
    }

    private PreflightResult Preflight(AppSettings settings)
    {
        var drive = driveDetectionService.Resolve(settings.SourcePath, settings.SourceDrive);
        if (drive.Status != DriveConnectionStatus.Connected)
        {
            return new(null, new(
                SynchronizationStatus.SsdNotConnected,
                drive.UserMessage ?? "SSD non collegato."));
        }

        var validation = pathSafetyValidator.Validate(
            drive.ResolvedSourcePath,
            settings.DestinationPath);
        if (!validation.IsValid)
        {
            var destinationUnavailable = validation.Issues.Any(issue =>
                issue.Code is PathValidationCode.DestinationDoesNotExist
                    or PathValidationCode.DestinationOutsideOneDrive
                    or PathValidationCode.DestinationPathInvalid
                    or PathValidationCode.DestinationRequired);
            return new(null, new(
                destinationUnavailable
                    ? SynchronizationStatus.DestinationUnavailable
                    : SynchronizationStatus.InvalidConfiguration,
                validation.Issues[0].UserMessage));
        }

        var availableBytes = diskSpaceProvider.GetAvailableBytes(settings.DestinationPath!);
        if (availableBytes is null)
        {
            return new(null, new(
                SynchronizationStatus.DestinationUnavailable,
                "Non è stato possibile verificare lo spazio disponibile nella destinazione."));
        }

        if (availableBytes < MinimumFreeBytes)
        {
            return new(null, new(
                SynchronizationStatus.DestinationUnavailable,
                "Spazio insufficiente nella destinazione. Libera almeno 256 MB e riprova."));
        }

        return new(drive.ResolvedSourcePath, null);
    }

    private async Task PersistProcessLogAsync(
        RobocopyResult result,
        CancellationToken cancellationToken)
    {
        foreach (var line in result.Output)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                await logger.WriteAsync(LogLevel.Information, line, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        foreach (var line in result.Errors)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                await logger.WriteAsync(LogLevel.Error, line, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        await logger.WriteAsync(
            result.Status == RobocopyOperationStatus.Failed ? LogLevel.Error : LogLevel.Information,
            result.UserMessage,
            cancellationToken).ConfigureAwait(false);
    }

    private static SynchronizationResult MapResult(RobocopyResult result, bool dryRun)
    {
        var status = result.Status switch
        {
            RobocopyOperationStatus.Completed => SynchronizationStatus.Completed,
            RobocopyOperationStatus.CompletedWithWarnings => SynchronizationStatus.CompletedWithWarnings,
            RobocopyOperationStatus.Cancelled => SynchronizationStatus.Cancelled,
            _ => SynchronizationStatus.Error,
        };
        var message = dryRun && status is SynchronizationStatus.Completed or SynchronizationStatus.CompletedWithWarnings
            ? "Anteprima completata. Controlla il log prima di confermare Mirror."
            : result.UserMessage;
        return new(status, message, result.Summary, result.Output);
    }

    private sealed record PreflightResult(
        string? ResolvedSourcePath,
        SynchronizationResult? Failure);
}
