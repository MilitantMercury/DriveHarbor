namespace DriveHarbor.Core.Configuration;

public sealed record AppSettings
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public string? SourcePath { get; init; }

    public string? DestinationPath { get; init; }

    public SyncMode Mode { get; init; } = SyncMode.Backup;

    public AppTheme Theme { get; init; } = AppTheme.System;

    public DriveFingerprint? SourceDrive { get; init; }

    public IReadOnlyList<string> Exclusions { get; init; } = Array.AsReadOnly<string>(
    [
        "$RECYCLE.BIN",
        "System Volume Information",
        "*.tmp",
        "~$*",
    ]);

    public string LogDirectory { get; init; } = AppPaths.DefaultLogDirectory;

    public DateTimeOffset? LastSynchronizationUtc { get; init; }

    public string? LastSynchronizationResult { get; init; }

    public long? LastCopiedFiles { get; init; }

    public static AppSettings CreateDefault() => new();
}
