using System.Diagnostics;
using System.IO.Compression;

return await Updater.RunAsync(args);

internal static class Updater
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length != 4 || !int.TryParse(args[2], out var processId)) return 2;
        var packagePath = Path.GetFullPath(args[0]);
        var installDirectory = Path.GetFullPath(args[1]);
        var appPath = Path.GetFullPath(args[3]);
        if (!File.Exists(packagePath) || !Directory.Exists(installDirectory)
            || !IsInside(appPath, installDirectory) || !File.Exists(appPath)) return 3;

        try
        {
            using var process = Process.GetProcessById(processId);
            if (!process.WaitForExit(60_000)) return 4;
        }
        catch (ArgumentException) { }

        var workRoot = Path.Combine(Path.GetDirectoryName(packagePath)!, $"apply-{Guid.NewGuid():N}");
        var staging = Path.Combine(workRoot, "staging");
        var backup = Path.Combine(workRoot, "backup");
        Directory.CreateDirectory(staging);
        Directory.CreateDirectory(backup);
        var createdFiles = new List<string>();
        try
        {
            ExtractSafely(packagePath, staging);
            foreach (var source in Directory.EnumerateFiles(staging, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(staging, source);
                var destination = Path.GetFullPath(Path.Combine(installDirectory, relative));
                if (!IsInside(destination, installDirectory)) throw new InvalidDataException("Unsafe update path.");
                var existing = Path.Combine(backup, relative);
                if (File.Exists(destination))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(existing)!);
                    File.Copy(destination, existing, true);
                }
                else
                {
                    createdFiles.Add(destination);
                }
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(source, destination, true);
            }
            Process.Start(new ProcessStartInfo(appPath) { UseShellExecute = true });
            return 0;
        }
        catch
        {
            foreach (var createdFile in createdFiles.Where(File.Exists)) File.Delete(createdFile);
            foreach (var source in Directory.EnumerateFiles(backup, "*", SearchOption.AllDirectories))
            {
                var destination = Path.Combine(installDirectory, Path.GetRelativePath(backup, source));
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(source, destination, true);
            }
            return 5;
        }
        finally
        {
            try { Directory.Delete(workRoot, true); } catch (IOException) { }
        }
    }

    private static void ExtractSafely(string packagePath, string staging)
    {
        using var archive = ZipFile.OpenRead(packagePath);
        foreach (var entry in archive.Entries)
        {
            var destination = Path.GetFullPath(Path.Combine(staging, entry.FullName));
            if (!IsInside(destination, staging)) throw new InvalidDataException("Archive contains an unsafe path.");
            if (string.IsNullOrEmpty(entry.Name)) Directory.CreateDirectory(destination);
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                entry.ExtractToFile(destination, true);
            }
        }
    }

    private static bool IsInside(string path, string directory) =>
        path.StartsWith(Path.TrimEndingDirectorySeparator(directory) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
}
