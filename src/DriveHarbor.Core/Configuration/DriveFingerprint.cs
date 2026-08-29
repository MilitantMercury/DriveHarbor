namespace DriveHarbor.Core.Configuration;

public sealed record DriveFingerprint
{
    public string? VolumeSerialNumber { get; init; }

    public string? VolumeLabel { get; init; }
}
