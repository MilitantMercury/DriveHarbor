namespace DriveHarbor.Core.Robocopy;

public sealed record RobocopyProgressInfo(
    double Percentage,
    TimeSpan? EstimatedRemaining);
