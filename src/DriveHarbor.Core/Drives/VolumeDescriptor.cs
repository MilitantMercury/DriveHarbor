namespace DriveHarbor.Core.Drives;

public sealed record VolumeDescriptor(
    string RootPath,
    string? VolumeGuidPath,
    string? VolumeSerialNumber,
    string? VolumeLabel,
    DriveType DriveType);
