namespace DriveHarbor.Core.Drives;

public enum DriveConnectionStatus
{
    NotConfigured,
    Connected,
    Disconnected,
    Ambiguous,
    StableIdentityUnavailable,
    SourceFolderUnavailable,
}
