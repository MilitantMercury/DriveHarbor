namespace DriveHarbor.Core.Updates;

public sealed record UpdateCheckResult(
    bool IsAvailable,
    string? Version = null,
    Uri? ReleaseUri = null,
    Uri? PackageUri = null,
    Uri? ChecksumUri = null,
    Uri? AttestationUri = null);
