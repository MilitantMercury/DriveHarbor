using DriveHarbor.Core.Robocopy;

namespace DriveHarbor.Core.Synchronization;

public sealed record SynchronizationResult(
    SynchronizationStatus Status,
    string UserMessage,
    RobocopySummary? Summary = null,
    IReadOnlyList<string>? Output = null);
