using DriveHarbor.Core.Configuration;
using DriveHarbor.Core.Robocopy;

namespace DriveHarbor.Core.Tests.Robocopy;

public sealed class RobocopyCommandBuilderTests
{
    [Fact]
    public void BackupUsesRecursiveCopyWithoutDeletionSwitch()
    {
        var arguments = RobocopyCommandBuilder.BuildArguments(new(
            @"E:\Source",
            @"C:\OneDrive\Backup"));

        Assert.Contains("/E", arguments);
        Assert.DoesNotContain("/MIR", arguments);
        Assert.DoesNotContain("/L", arguments);
        Assert.Contains("/XJ", arguments);
        Assert.Contains("/R:2", arguments);
    }

    [Fact]
    public void MirrorExecutionRequiresExplicitConfirmation()
    {
        var request = new RobocopyRequest(
            @"E:\Source",
            @"C:\OneDrive\Backup",
            SyncMode.Mirror);

        var exception = Assert.Throws<InvalidOperationException>(
            () => RobocopyCommandBuilder.BuildArguments(request));

        Assert.Contains("explicitly confirmed", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfirmedMirrorUsesMirrorSwitch()
    {
        var arguments = RobocopyCommandBuilder.BuildArguments(new(
            @"E:\Source",
            @"C:\OneDrive\Backup",
            SyncMode.Mirror,
            MirrorConfirmed: true));

        Assert.Contains("/MIR", arguments);
        Assert.DoesNotContain("/E", arguments);
        Assert.DoesNotContain("/L", arguments);
    }

    [Fact]
    public void MirrorPreviewCannotModifyFilesAndDoesNotRequireConfirmation()
    {
        var arguments = RobocopyCommandBuilder.BuildArguments(new(
            @"E:\Source",
            @"C:\OneDrive\Backup",
            SyncMode.Mirror,
            DryRun: true));

        Assert.Contains("/MIR", arguments);
        Assert.Contains("/L", arguments);
    }

    [Fact]
    public void ExclusionsArePassedAsSeparateArgumentsForFilesAndDirectories()
    {
        var arguments = RobocopyCommandBuilder.BuildArguments(new(
            @"E:\Source",
            @"C:\OneDrive\Backup",
            Exclusions: ["*.tmp", "System Volume Information"]));

        Assert.Equal(2, arguments.Count(argument => argument == "/XD"));
        Assert.Equal(2, arguments.Count(argument => argument == "/XF"));
        Assert.Contains("*.tmp", arguments);
        Assert.Contains("System Volume Information", arguments);
    }

    [Theory]
    [InlineData(@"E:\Source", @"E:\Source")]
    [InlineData(@"E:\Source", @"E:\Source\Nested")]
    [InlineData(@"E:\Source\Nested", @"E:\Source")]
    public void EqualOrNestedPathsAreRejected(string source, string destination)
    {
        var request = new RobocopyRequest(source, destination);

        Assert.Throws<ArgumentException>(() => RobocopyCommandBuilder.BuildArguments(request));
    }

    [Theory]
    [InlineData("")]
    [InlineData("/MIR")]
    [InlineData("bad\nvalue")]
    public void InvalidExclusionsAreRejected(string exclusion)
    {
        var request = new RobocopyRequest(
            @"E:\Source",
            @"C:\OneDrive\Backup",
            Exclusions: [exclusion]);

        Assert.Throws<ArgumentException>(() => RobocopyCommandBuilder.BuildArguments(request));
    }
}
