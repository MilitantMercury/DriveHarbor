namespace DriveHarbor.Core.Validation;

public enum PathValidationCode
{
    SourceRequired,
    DestinationRequired,
    SourcePathInvalid,
    DestinationPathInvalid,
    SourceDoesNotExist,
    DestinationDoesNotExist,
    PathsAreEqual,
    PathsAreNested,
    PathContainsReparsePoint,
    DestinationOutsideOneDrive,
}
