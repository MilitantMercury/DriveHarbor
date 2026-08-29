using DriveHarbor.Core.Validation;

namespace DriveHarbor.Core.Tests.Validation;

public sealed class LogDirectorySafetyValidatorTests
{
    [Theory]
    [InlineData(@"E:\Source\Logs", @"E:\Source", @"C:\OneDrive\Backup")]
    [InlineData(@"C:\OneDrive\Backup\Logs", @"E:\Source", @"C:\OneDrive\Backup")]
    [InlineData(@"E:\", @"E:\Source", @"C:\OneDrive\Backup")]
    public void OverlappingLogDirectoryIsRejected(string log, string source, string destination)
    {
        var result = LogDirectorySafetyValidator.Validate(log, source, destination);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void SeparateLogDirectoryIsAccepted()
    {
        var result = LogDirectorySafetyValidator.Validate(
            @"C:\Users\Example\AppData\Local\DriveHarbor\Logs",
            @"E:\Source",
            @"C:\Users\Example\OneDrive\Backup");

        Assert.True(result.IsValid);
    }
}
