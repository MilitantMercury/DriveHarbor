namespace DriveHarbor.Core.Robocopy;

public sealed record RobocopySummary(
    long? TotalFiles = null,
    long? CopiedFiles = null,
    long? SkippedFiles = null,
    long? MismatchedFiles = null,
    long? FailedFiles = null,
    long? ExtraFiles = null);
