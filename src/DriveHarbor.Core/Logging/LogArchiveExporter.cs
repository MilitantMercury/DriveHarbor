using System.IO.Compression;

namespace DriveHarbor.Core.Logging;

public static class LogArchiveExporter
{
    public static int Export(string logDirectory, string destinationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        var sourceDirectory = Path.GetFullPath(logDirectory);
        var destination = Path.GetFullPath(destinationPath);
        if (!Directory.Exists(sourceDirectory)) return 0;
        if (!string.Equals(Path.GetExtension(destination), ".zip", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The log archive must use the .zip extension.", nameof(destinationPath));

        var logFiles = Directory.EnumerateFiles(sourceDirectory, "*.log", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (logFiles.Length == 0) return 0;

        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        var temporaryPath = $"{destination}.{Guid.NewGuid():N}.tmp";
        try
        {
            using (var archive = ZipFile.Open(temporaryPath, ZipArchiveMode.Create))
            {
                foreach (var logFile in logFiles)
                {
                    archive.CreateEntryFromFile(logFile, Path.GetFileName(logFile), CompressionLevel.Optimal);
                }
            }

            File.Move(temporaryPath, destination, true);
            return logFiles.Length;
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
}
