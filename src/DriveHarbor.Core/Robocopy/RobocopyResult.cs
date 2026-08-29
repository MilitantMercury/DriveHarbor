namespace DriveHarbor.Core.Robocopy;

public sealed record RobocopyResult(
    RobocopyOperationStatus Status,
    int? ExitCode,
    string UserMessage,
    RobocopySummary Summary,
    IReadOnlyList<string> Output,
    IReadOnlyList<string> Errors,
    RobocopyFailureKind FailureKind = RobocopyFailureKind.None);
