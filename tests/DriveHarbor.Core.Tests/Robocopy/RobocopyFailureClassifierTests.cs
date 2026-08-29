using DriveHarbor.Core.Robocopy;

namespace DriveHarbor.Core.Tests.Robocopy;

public sealed class RobocopyFailureClassifierTests
{
    [Theory]
    [InlineData("ERROR 112 (0x00000070)", RobocopyFailureKind.InsufficientSpace)]
    [InlineData("ERROR 5 (0x00000005) Access is denied", RobocopyFailureKind.AccessDenied)]
    [InlineData("ERROR 32 file being used by another process", RobocopyFailureKind.FileLocked)]
    [InlineData("ERROR 206 filename or extension is too long", RobocopyFailureKind.PathTooLong)]
    [InlineData("ERROR 3 cannot find the path", RobocopyFailureKind.PathUnavailable)]
    [InlineData("unrecognized failure", RobocopyFailureKind.Unknown)]
    public void TechnicalOutputMapsToUserFacingFailure(string line, RobocopyFailureKind expected)
    {
        var result = RobocopyFailureClassifier.Classify([line], []);

        Assert.Equal(expected, result.Kind);
        Assert.DoesNotContain("ERROR", result.UserMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ItalianAccessDeniedOutputIsRecognized()
    {
        var result = RobocopyFailureClassifier.Classify([], ["Accesso negato"]);

        Assert.Equal(RobocopyFailureKind.AccessDenied, result.Kind);
    }
}
