namespace DriveHarbor.Core.Validation;

public static class LogDirectorySafetyValidator
{
    public static LogDirectoryValidationResult Validate(
        string? logDirectory,
        string? sourcePath,
        string? destinationPath)
    {
        var log = Normalize(logDirectory);
        var source = Normalize(sourcePath);
        var destination = Normalize(destinationPath);
        if (log is null)
        {
            return new(false, "Scegli una posizione valida per i log.");
        }

        if (Conflicts(log, source) || Conflicts(log, destination))
        {
            return new(
                false,
                "La cartella dei log deve essere separata da sorgente e destinazione per evitare loop di sincronizzazione.");
        }

        return new(true);
    }

    private static bool Conflicts(string log, string? dataPath) =>
        dataPath is not null
        && (string.Equals(log, dataPath, StringComparison.OrdinalIgnoreCase)
            || IsContainedBy(log, dataPath)
            || IsContainedBy(dataPath, log));

    private static string? Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
        {
            return null;
        }

        try
        {
            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        }
        catch (Exception exception) when (
            exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
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
}
