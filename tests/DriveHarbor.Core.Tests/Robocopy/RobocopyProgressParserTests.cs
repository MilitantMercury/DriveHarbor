using DriveHarbor.Core.Robocopy;

namespace DriveHarbor.Core.Tests.Robocopy;

public sealed class RobocopyProgressParserTests
{
    [Theory]
    [InlineData(" 42.5%", 42.5)]
    [InlineData(" 42,5%", 42.5)]
    [InlineData("100%", 100)]
    public void ParsesCurrentFilePercentage(string line, double expected)
    {
        var parsed = RobocopyProgressParser.TryParse(line, out var progress);

        Assert.True(parsed);
        Assert.NotNull(progress);
        Assert.Equal(expected, progress.Percentage);
    }

    [Fact]
    public void ParsesEstimatedRemainingTimeWhenRobocopyProvidesIt()
    {
        var parsed = RobocopyProgressParser.TryParse(" 67.0%  ETA 0:02:15", out var progress);

        Assert.True(parsed);
        Assert.Equal(TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(15), progress!.EstimatedRemaining);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Nuovo file 123 esempio.txt")]
    [InlineData("RIEPILOGO")]
    public void IgnoresLinesWithoutProgress(string line)
    {
        Assert.False(RobocopyProgressParser.TryParse(line, out var progress));
        Assert.Null(progress);
    }
}
