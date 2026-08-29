using DriveHarbor.Core.Robocopy;

namespace DriveHarbor.Core.Tests.Robocopy;

public sealed class RobocopyExitCodeInterpreterTests
{
    [Theory]
    [InlineData(0, RobocopyOperationStatus.Completed)]
    [InlineData(1, RobocopyOperationStatus.Completed)]
    [InlineData(2, RobocopyOperationStatus.CompletedWithWarnings)]
    [InlineData(3, RobocopyOperationStatus.CompletedWithWarnings)]
    [InlineData(4, RobocopyOperationStatus.CompletedWithWarnings)]
    [InlineData(5, RobocopyOperationStatus.CompletedWithWarnings)]
    [InlineData(6, RobocopyOperationStatus.CompletedWithWarnings)]
    [InlineData(7, RobocopyOperationStatus.CompletedWithWarnings)]
    [InlineData(8, RobocopyOperationStatus.Failed)]
    [InlineData(16, RobocopyOperationStatus.Failed)]
    [InlineData(32, RobocopyOperationStatus.Failed)]
    [InlineData(-1, RobocopyOperationStatus.Failed)]
    public void ExitCodeMapsToExpectedStatus(int exitCode, RobocopyOperationStatus expected)
    {
        Assert.Equal(expected, RobocopyExitCodeInterpreter.GetStatus(exitCode));
    }

    [Fact]
    public void TechnicalExitCodeIsNotExposedInUserMessage()
    {
        var message = RobocopyExitCodeInterpreter.GetUserMessage(8);

        Assert.DoesNotContain("8", message, StringComparison.Ordinal);
        Assert.DoesNotContain("exit code", message, StringComparison.OrdinalIgnoreCase);
    }
}
