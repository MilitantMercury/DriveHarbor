using DriveHarbor.Core.Robocopy;
using DriveHarbor.Core.Tests.Infrastructure;

namespace DriveHarbor.Core.Tests.Robocopy;

public sealed class RobocopyRunnerTests
{
    [Fact]
    public async Task DryRunUsesRealRobocopyWithoutChangingDestination()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var source = temporaryDirectory.CreateDirectory("Source");
        var destination = temporaryDirectory.CreateDirectory("Destination");
        await File.WriteAllTextAsync(Path.Combine(source, "example.txt"), "test content");
        var runner = new RobocopyRunner();

        var result = await runner.RunAsync(new(
            source,
            destination,
            DryRun: true));

        Assert.NotEqual(RobocopyOperationStatus.Failed, result.Status);
        Assert.NotEqual(RobocopyOperationStatus.Cancelled, result.Status);
        Assert.NotEmpty(result.Output);
        Assert.False(File.Exists(Path.Combine(destination, "example.txt")));
    }
}
