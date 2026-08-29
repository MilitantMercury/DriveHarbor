namespace DriveHarbor.Core.Synchronization;

public enum SynchronizationStatus
{
    Completed,
    CompletedWithWarnings,
    Cancelled,
    Error,
    SsdNotConnected,
    DestinationUnavailable,
    InvalidConfiguration,
    MirrorConfirmationRequired,
}
