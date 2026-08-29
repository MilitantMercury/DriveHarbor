namespace DriveHarbor.Core.Validation;

public sealed record LogDirectoryValidationResult(bool IsValid, string? UserMessage = null);
