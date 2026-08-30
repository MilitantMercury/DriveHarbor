using DriveHarbor.Core.Updates;

namespace DriveHarbor.Core.Tests.Updates;

public sealed class SemanticVersionTests
{
    [Theory]
    [InlineData("v1.0.0-beta.2", "1.0.0-beta.1")]
    [InlineData("1.0.0", "1.0.0-beta.9")]
    [InlineData("1.1.0", "1.0.9")]
    public void NewerVersionsCompareGreater(string newer, string older)
    {
        Assert.True(SemanticVersion.TryParse(newer, out var left));
        Assert.True(SemanticVersion.TryParse(older, out var right));
        Assert.True(left!.CompareTo(right) > 0);
    }

    [Fact]
    public void BuildMetadataDoesNotChangeDisplayedVersion()
    {
        Assert.True(SemanticVersion.TryParse("1.0.0-beta.1+commit", out var version));
        Assert.Equal("1.0.0-beta.1", version!.ToString());
    }
}
