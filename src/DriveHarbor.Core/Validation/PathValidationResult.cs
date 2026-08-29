namespace DriveHarbor.Core.Validation;

public sealed record PathValidationResult(IReadOnlyList<PathValidationIssue> Issues)
{
    public bool IsValid => Issues.Count == 0;
}
