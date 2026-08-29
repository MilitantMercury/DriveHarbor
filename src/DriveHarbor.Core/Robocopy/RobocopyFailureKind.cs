namespace DriveHarbor.Core.Robocopy;

public enum RobocopyFailureKind
{
    None,
    InsufficientSpace,
    AccessDenied,
    FileLocked,
    PathTooLong,
    PathUnavailable,
    EngineUnavailable,
    Unknown,
}
