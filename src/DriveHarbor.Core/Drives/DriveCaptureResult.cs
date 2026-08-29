using DriveHarbor.Core.Configuration;

namespace DriveHarbor.Core.Drives;

public sealed record DriveCaptureResult(
    DriveCaptureStatus Status,
    DriveFingerprint? Fingerprint = null,
    string? UserMessage = null);
