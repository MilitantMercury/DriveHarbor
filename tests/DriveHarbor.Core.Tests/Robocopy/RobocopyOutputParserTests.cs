using DriveHarbor.Core.Robocopy;

namespace DriveHarbor.Core.Tests.Robocopy;

public sealed class RobocopyOutputParserTests
{
    [Theory]
    [InlineData("    Files :       124       12       112         0         0         0")]
    [InlineData("     File :       124       12       112         0         0         0")]
    public void FileSummaryExtractsFriendlyCounts(string summaryLine)
    {
        var summary = RobocopyOutputParser.Parse(["header", summaryLine]);

        Assert.Equal(124, summary.TotalFiles);
        Assert.Equal(12, summary.CopiedFiles);
        Assert.Equal(112, summary.SkippedFiles);
        Assert.Equal(0, summary.MismatchedFiles);
        Assert.Equal(0, summary.FailedFiles);
        Assert.Equal(0, summary.ExtraFiles);
    }

    [Fact]
    public void LocalizedOrUnknownOutputLeavesCountsUnavailable()
    {
        var summary = RobocopyOutputParser.Parse(["riepilogo non riconosciuto"]);

        Assert.Null(summary.TotalFiles);
        Assert.Null(summary.CopiedFiles);
        Assert.Null(summary.ExtraFiles);
    }
}
