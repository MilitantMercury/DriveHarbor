using DriveHarbor.Core.Configuration;

namespace DriveHarbor.Core.Robocopy;

public sealed record RobocopyRequest(
    string SourcePath,
    string DestinationPath,
    SyncMode Mode = SyncMode.Backup,
    IReadOnlyList<string>? Exclusions = null,
    bool DryRun = false,
    bool MirrorConfirmed = false);
