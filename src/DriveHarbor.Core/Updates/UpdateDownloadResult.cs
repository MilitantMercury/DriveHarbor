namespace DriveHarbor.Core.Updates;

public sealed record UpdateDownloadResult(bool Succeeded, string UserMessage, string? PackagePath = null);
