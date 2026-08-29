using DriveHarbor.Core.Tests.Infrastructure;
using DriveHarbor.Core.Validation;

namespace DriveHarbor.Core.Tests.Validation;

public sealed class PathSafetyValidatorTests
{
    [Fact]
    public void EmptyPathsAreRejected()
    {
        var validator = new PathSafetyValidator(new StubOneDriveRootProvider([]));

        var result = validator.Validate(null, " ");

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == PathValidationCode.SourceRequired);
        Assert.Contains(result.Issues, issue => issue.Code == PathValidationCode.DestinationRequired);
    }

    [Fact]
    public void SamePathIsRejected()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var folder = temporaryDirectory.CreateDirectory("OneDrive");
        var validator = new PathSafetyValidator(new StubOneDriveRootProvider([folder]));

        var result = validator.Validate(folder, folder + Path.DirectorySeparatorChar);

        Assert.Contains(result.Issues, issue => issue.Code == PathValidationCode.PathsAreEqual);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NestedPathsAreRejected(bool destinationInsideSource)
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var outer = temporaryDirectory.CreateDirectory("OneDrive");
        var inner = temporaryDirectory.CreateDirectory(Path.Combine("OneDrive", "Nested"));
        var validator = new PathSafetyValidator(new StubOneDriveRootProvider([outer]));
        var source = destinationInsideSource ? outer : inner;
        var destination = destinationInsideSource ? inner : outer;

        var result = validator.Validate(source, destination);

        Assert.Contains(result.Issues, issue => issue.Code == PathValidationCode.PathsAreNested);
    }

    [Fact]
    public void MissingDirectoriesAreRejected()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var oneDrive = temporaryDirectory.CreateDirectory("OneDrive");
        var missingSource = Path.Combine(temporaryDirectory.FullPath, "MissingSource");
        var missingDestination = Path.Combine(oneDrive, "MissingDestination");
        var validator = new PathSafetyValidator(new StubOneDriveRootProvider([oneDrive]));

        var result = validator.Validate(missingSource, missingDestination);

        Assert.Contains(result.Issues, issue => issue.Code == PathValidationCode.SourceDoesNotExist);
        Assert.Contains(result.Issues, issue => issue.Code == PathValidationCode.DestinationDoesNotExist);
    }

    [Fact]
    public void DestinationOutsideOneDriveIsRejected()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var source = temporaryDirectory.CreateDirectory("Source");
        var oneDrive = temporaryDirectory.CreateDirectory("OneDrive");
        var destination = temporaryDirectory.CreateDirectory("OtherDestination");
        var validator = new PathSafetyValidator(new StubOneDriveRootProvider([oneDrive]));

        var result = validator.Validate(source, destination);

        Assert.Contains(result.Issues, issue => issue.Code == PathValidationCode.DestinationOutsideOneDrive);
    }

    [Fact]
    public void SeparateSourceAndOneDriveDestinationAreAccepted()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var source = temporaryDirectory.CreateDirectory("Source");
        var oneDrive = temporaryDirectory.CreateDirectory("OneDrive");
        var destination = temporaryDirectory.CreateDirectory(Path.Combine("OneDrive", "DriveHarbor"));
        var validator = new PathSafetyValidator(new StubOneDriveRootProvider([oneDrive]));

        var result = validator.Validate(source, destination);

        Assert.True(result.IsValid);
    }

    private sealed class StubOneDriveRootProvider(IReadOnlyList<string> roots) : IOneDriveRootProvider
    {
        public IReadOnlyList<string> GetAvailableRoots() => roots;
    }
}
