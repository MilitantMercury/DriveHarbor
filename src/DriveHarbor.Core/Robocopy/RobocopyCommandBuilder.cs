using DriveHarbor.Core.Configuration;

namespace DriveHarbor.Core.Robocopy;

public static class RobocopyCommandBuilder
{
    public static IReadOnlyList<string> BuildArguments(RobocopyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var source = NormalizeDirectory(request.SourcePath, nameof(request.SourcePath));
        var destination = NormalizeDirectory(request.DestinationPath, nameof(request.DestinationPath));
        EnsurePathsAreSeparated(source, destination);

        if (!Enum.IsDefined(request.Mode))
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Unknown synchronization mode.");
        }

        if (request.Mode == SyncMode.Mirror && !request.DryRun && !request.MirrorConfirmed)
        {
            throw new InvalidOperationException("Mirror must be explicitly confirmed before execution.");
        }

        var arguments = new List<string>
        {
            source,
            destination,
            request.Mode == SyncMode.Mirror ? "/MIR" : "/E",
            "/COPY:DAT",
            "/DCOPY:DAT",
            "/R:2",
            "/W:2",
            "/Z",
            "/XJ",
            "/NP",
            "/BYTES",
        };

        if (request.DryRun)
        {
            arguments.Add("/L");
        }

        foreach (var exclusion in request.Exclusions ?? [])
        {
            ValidateExclusion(exclusion);
            arguments.Add("/XD");
            arguments.Add(exclusion);
            arguments.Add("/XF");
            arguments.Add(exclusion);
        }

        return arguments;
    }

    private static string NormalizeDirectory(string path, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
        {
            throw new ArgumentException("A fully qualified directory path is required.", parameterName);
        }

        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    }

    private static void EnsurePathsAreSeparated(string source, string destination)
    {
        if (string.Equals(source, destination, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Source and destination cannot be equal.");
        }

        if (IsContainedBy(source, destination) || IsContainedBy(destination, source))
        {
            throw new ArgumentException("Source and destination cannot contain each other.");
        }
    }

    private static bool IsContainedBy(string candidate, string parent)
    {
        var relative = Path.GetRelativePath(parent, candidate);
        return !Path.IsPathFullyQualified(relative)
            && !relative.Equals(".", StringComparison.Ordinal)
            && !relative.Equals("..", StringComparison.Ordinal)
            && !relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            && !relative.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal);
    }

    private static void ValidateExclusion(string exclusion)
    {
        if (string.IsNullOrWhiteSpace(exclusion)
            || exclusion[0] == '/'
            || exclusion.Contains('\r', StringComparison.Ordinal)
            || exclusion.Contains('\n', StringComparison.Ordinal))
        {
            throw new ArgumentException("An exclusion contains an invalid value.", nameof(exclusion));
        }
    }
}
