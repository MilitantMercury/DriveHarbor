namespace DriveHarbor.Core.Configuration;

public sealed record DriveFingerprint
{
    public string? VolumeGuidPath { get; init; }

    public string? VolumeSerialNumber { get; init; }

    public string? VolumeLabel { get; init; }
}
