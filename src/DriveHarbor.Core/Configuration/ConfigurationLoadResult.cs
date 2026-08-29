namespace DriveHarbor.Core.Configuration;

public sealed record ConfigurationLoadResult(
    AppSettings Settings,
    ConfigurationLoadStatus Status,
    string? UserMessage = null);
