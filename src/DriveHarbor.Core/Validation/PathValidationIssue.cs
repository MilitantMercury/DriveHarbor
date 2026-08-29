namespace DriveHarbor.Core.Validation;

public sealed record PathValidationIssue(PathValidationCode Code, string UserMessage);
