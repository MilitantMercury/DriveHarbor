namespace DriveHarbor.Core.Validation;

public sealed class PathSafetyValidator(IOneDriveRootProvider oneDriveRootProvider)
{
    public PathValidationResult Validate(string? sourcePath, string? destinationPath)
    {
        var issues = new List<PathValidationIssue>();
        var source = NormalizePath(sourcePath, isSource: true, issues);
        var destination = NormalizePath(destinationPath, isSource: false, issues);

        if (source is not null && !Directory.Exists(source))
        {
            issues.Add(new(
                PathValidationCode.SourceDoesNotExist,
                "La cartella sorgente non è disponibile."));
        }

        if (destination is not null && !Directory.Exists(destination))
        {
            issues.Add(new(
                PathValidationCode.DestinationDoesNotExist,
                "La cartella di destinazione non è disponibile."));
        }

        if (source is not null && destination is not null)
        {
            if (string.Equals(source, destination, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new(
                    PathValidationCode.PathsAreEqual,
                    "Sorgente e destinazione devono essere cartelle diverse."));
            }
            else if (IsContainedBy(source, destination) || IsContainedBy(destination, source))
            {
                issues.Add(new(
                    PathValidationCode.PathsAreNested,
                    "Una cartella non può essere contenuta nell'altra."));
            }
        }

        if (destination is not null && !IsInsideKnownOneDriveRoot(destination))
        {
            issues.Add(new(
                PathValidationCode.DestinationOutsideOneDrive,
                "Scegli una destinazione all'interno di una cartella OneDrive disponibile."));
        }

        return new(issues);
    }

    private static string? NormalizePath(
        string? path,
        bool isSource,
        List<PathValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            issues.Add(new(
                isSource ? PathValidationCode.SourceRequired : PathValidationCode.DestinationRequired,
                isSource ? "Seleziona una cartella sorgente." : "Seleziona una cartella di destinazione."));
            return null;
        }

        try
        {
            if (!Path.IsPathFullyQualified(path))
            {
                throw new ArgumentException("Path is not fully qualified.", nameof(path));
            }

            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        }
        catch (Exception exception) when (
            exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            issues.Add(new(
                isSource ? PathValidationCode.SourcePathInvalid : PathValidationCode.DestinationPathInvalid,
                isSource ? "Il percorso della sorgente non è valido." : "Il percorso della destinazione non è valido."));
            return null;
        }
    }

    private bool IsInsideKnownOneDriveRoot(string destination)
    {
        foreach (var root in oneDriveRootProvider.GetAvailableRoots())
        {
            string normalizedRoot;
            try
            {
                normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
            }
            catch (Exception exception) when (
                exception is ArgumentException or NotSupportedException or PathTooLongException)
            {
                continue;
            }

            if (Directory.Exists(normalizedRoot)
                && (string.Equals(destination, normalizedRoot, StringComparison.OrdinalIgnoreCase)
                    || IsContainedBy(destination, normalizedRoot)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsContainedBy(string candidate, string parent)
    {
        var relativePath = Path.GetRelativePath(parent, candidate);
        return !Path.IsPathFullyQualified(relativePath)
            && !string.Equals(relativePath, ".", StringComparison.Ordinal)
            && !relativePath.Equals("..", StringComparison.Ordinal)
            && !relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            && !relativePath.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal);
    }
}
