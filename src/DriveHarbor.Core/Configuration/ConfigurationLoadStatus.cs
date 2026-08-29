namespace DriveHarbor.Core.Configuration;

public enum ConfigurationLoadStatus
{
    Loaded,
    DefaultsUsed,
    InvalidFile,
    UnsupportedVersion,
}
