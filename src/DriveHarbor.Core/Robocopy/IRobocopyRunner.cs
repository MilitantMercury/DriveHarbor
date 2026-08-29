namespace DriveHarbor.Core.Robocopy;

public interface IRobocopyRunner
{
    Task<RobocopyResult> RunAsync(
        RobocopyRequest request,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}
