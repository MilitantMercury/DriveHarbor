namespace DriveHarbor.Core.Drives;

public sealed record DriveResolutionResult(
    DriveConnectionStatus Status,
    string? ResolvedSourcePath = null,
    VolumeDescriptor? Volume = null,
    string? UserMessage = null);
