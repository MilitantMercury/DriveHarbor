using DriveHarbor.Core.Configuration;

namespace DriveHarbor.Core.Drives;

public sealed class DriveDetectionService(IVolumeCatalog volumeCatalog)
{
    public DriveCaptureResult Capture(string? sourcePath)
    {
        var normalizedSource = TryNormalizeSourcePath(sourcePath);
        if (normalizedSource is null)
        {
            return new(
                DriveCaptureStatus.SourcePathInvalid,
                UserMessage: "La cartella sorgente non è valida o non è disponibile.");
        }

        var sourceRoot = Path.GetPathRoot(normalizedSource)!;
        var rootMatches = volumeCatalog.GetAvailableVolumes()
            .Where(candidate => SamePath(candidate.RootPath, sourceRoot))
            .ToArray();
        if (rootMatches.Length != 1)
        {
            return new(
                DriveCaptureStatus.VolumeUnavailable,
                UserMessage: "Il volume della cartella sorgente non è disponibile.");
        }

        var volume = rootMatches[0];

        if (string.IsNullOrWhiteSpace(volume.VolumeGuidPath)
            && string.IsNullOrWhiteSpace(volume.VolumeSerialNumber))
        {
            return new(
                DriveCaptureStatus.StableIdentityUnavailable,
                UserMessage: "Windows non ha fornito un identificatore stabile per questo volume.");
        }

        return new(
            DriveCaptureStatus.Captured,
            new DriveFingerprint
            {
                VolumeGuidPath = volume.VolumeGuidPath,
                VolumeSerialNumber = volume.VolumeSerialNumber,
                VolumeLabel = volume.VolumeLabel,
            });
    }

    public DriveResolutionResult Resolve(string? configuredSourcePath, DriveFingerprint? fingerprint)
    {
        if (string.IsNullOrWhiteSpace(configuredSourcePath) || fingerprint is null)
        {
            return new(
                DriveConnectionStatus.NotConfigured,
                UserMessage: "Configura una cartella sul tuo SSD.");
        }

        if (string.IsNullOrWhiteSpace(fingerprint.VolumeGuidPath)
            && string.IsNullOrWhiteSpace(fingerprint.VolumeSerialNumber))
        {
            return new(
                DriveConnectionStatus.StableIdentityUnavailable,
                UserMessage: "L'identità salvata del volume non è sufficiente per sincronizzare in sicurezza.");
        }

        var originalSource = TryNormalizeAbsolutePath(configuredSourcePath);
        if (originalSource is null)
        {
            return new(
                DriveConnectionStatus.NotConfigured,
                UserMessage: "Il percorso sorgente configurato non è valido.");
        }

        var volumes = volumeCatalog.GetAvailableVolumes();
        var matches = FindMatches(volumes, fingerprint);
        if (matches.Length == 0)
        {
            return new(
                DriveConnectionStatus.Disconnected,
                UserMessage: "SSD non collegato.");
        }

        if (matches.Length > 1)
        {
            return new(
                DriveConnectionStatus.Ambiguous,
                UserMessage: "Più volumi corrispondono all'SSD configurato. Riconfigura la sorgente.");
        }

        var volume = matches[0];
        var resolvedSource = RebaseSourcePath(originalSource, volume.RootPath);
        if (resolvedSource is null || !Directory.Exists(resolvedSource))
        {
            return new(
                DriveConnectionStatus.SourceFolderUnavailable,
                Volume: volume,
                UserMessage: "SSD collegato, ma la cartella sorgente non è disponibile.");
        }

        return new(
            DriveConnectionStatus.Connected,
            resolvedSource,
            volume,
            "SSD collegato.");
    }

    private static VolumeDescriptor[] FindMatches(
        IReadOnlyList<VolumeDescriptor> volumes,
        DriveFingerprint fingerprint)
    {
        if (!string.IsNullOrWhiteSpace(fingerprint.VolumeGuidPath))
        {
            var guidMatches = volumes
                .Where(volume => SameIdentifier(volume.VolumeGuidPath, fingerprint.VolumeGuidPath))
                .ToArray();
            if (guidMatches.Length > 0)
            {
                return guidMatches;
            }

        }

        if (string.IsNullOrWhiteSpace(fingerprint.VolumeSerialNumber))
        {
            return [];
        }

        var serialMatches = volumes
            .Where(volume => SameIdentifier(volume.VolumeSerialNumber, fingerprint.VolumeSerialNumber))
            .Where(volume => string.IsNullOrWhiteSpace(fingerprint.VolumeGuidPath)
                || string.IsNullOrWhiteSpace(volume.VolumeGuidPath))
            .ToArray();
        if (serialMatches.Length <= 1 || string.IsNullOrWhiteSpace(fingerprint.VolumeLabel))
        {
            return serialMatches;
        }

        var labelMatches = serialMatches
            .Where(volume => SameIdentifier(volume.VolumeLabel, fingerprint.VolumeLabel))
            .ToArray();
        return labelMatches.Length > 0 ? labelMatches : serialMatches;
    }

    private static string? RebaseSourcePath(string originalSource, string currentRoot)
    {
        var originalRoot = Path.GetPathRoot(originalSource);
        if (string.IsNullOrWhiteSpace(originalRoot))
        {
            return null;
        }

        var relativePath = Path.GetRelativePath(originalRoot, originalSource);
        if (Path.IsPathFullyQualified(relativePath)
            || relativePath.Equals("..", StringComparison.Ordinal)
            || relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        {
            return null;
        }

        return Path.GetFullPath(Path.Combine(currentRoot, relativePath));
    }

    private static string? TryNormalizeSourcePath(string? sourcePath)
    {
        var normalized = TryNormalizeAbsolutePath(sourcePath);
        return normalized is not null && Directory.Exists(normalized) ? normalized : null;
    }

    private static string? TryNormalizeAbsolutePath(string? path)
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

    private static bool SamePath(string left, string right) =>
        string.Equals(
            Path.TrimEndingDirectorySeparator(left),
            Path.TrimEndingDirectorySeparator(right),
            StringComparison.OrdinalIgnoreCase);

    private static bool SameIdentifier(string? left, string? right) =>
        !string.IsNullOrWhiteSpace(left)
        && !string.IsNullOrWhiteSpace(right)
        && string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
}
