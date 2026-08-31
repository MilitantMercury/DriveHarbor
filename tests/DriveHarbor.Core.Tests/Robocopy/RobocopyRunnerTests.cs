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
        const string fileName = "Questa è una prova.txt";
        await File.WriteAllTextAsync(Path.Combine(source, fileName), "test content");
        var runner = new RobocopyRunner();

        var result = await runner.RunAsync(new(
            source,
            destination,
            DryRun: true));

        Assert.NotEqual(RobocopyOperationStatus.Failed, result.Status);
        Assert.NotEqual(RobocopyOperationStatus.Cancelled, result.Status);
        Assert.NotEmpty(result.Output);
        Assert.DoesNotContain(result.Output, line => line.Contains('\0', StringComparison.Ordinal));
        Assert.DoesNotContain(result.Output, line => line.Contains('\uFFFD', StringComparison.Ordinal));
        Assert.Contains(result.Output, line => line.Contains(fileName, StringComparison.Ordinal));
        Assert.Equal(1, result.Summary.TotalFiles);
        Assert.Equal(1, result.Summary.CopiedFiles);
        Assert.Equal(0, result.Summary.FailedFiles);
        Assert.False(File.Exists(Path.Combine(destination, fileName)));
    }
}
