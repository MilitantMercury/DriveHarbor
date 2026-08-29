namespace DriveHarbor.Core.Tests;

public sealed class ProjectConventionsTests
{
    [Fact]
    public void TestsTargetDotNetTen()
    {
        Assert.StartsWith("10.", Environment.Version.ToString(), StringComparison.Ordinal);
    }
}
