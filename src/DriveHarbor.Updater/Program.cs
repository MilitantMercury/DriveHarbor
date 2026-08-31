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
        var resultPath = Path.Combine(Path.GetDirectoryName(packagePath)!, "last-update-result.txt");
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
            WriteResult(resultPath, true, "DriveHarbor è stato aggiornato correttamente.");
            StartApplication(appPath);
            return 0;
        }
        catch (Exception exception)
        {
            var rollbackSucceeded = true;
            try
            {
                foreach (var createdFile in createdFiles.Where(File.Exists)) File.Delete(createdFile);
                foreach (var source in Directory.EnumerateFiles(backup, "*", SearchOption.AllDirectories))
                {
                    var destination = Path.Combine(installDirectory, Path.GetRelativePath(backup, source));
                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    File.Copy(source, destination, true);
                }
            }
            catch (Exception) { rollbackSucceeded = false; }

            var message = rollbackSucceeded
                ? $"Aggiornamento non riuscito; la versione precedente è stata ripristinata. Dettaglio: {exception.Message}"
                : $"Aggiornamento e ripristino non completati. Reinstalla DriveHarbor manualmente. Dettaglio: {exception.Message}";
            WriteResult(resultPath, false, message);
            StartApplication(appPath);
            return 5;
        }
        finally
        {
            try { Directory.Delete(workRoot, true); } catch (IOException) { }
        }
    }

    private static void StartApplication(string appPath)
    {
        try
        {
            var startInfo = new ProcessStartInfo("explorer.exe") { UseShellExecute = true };
            startInfo.ArgumentList.Add(appPath);
            Process.Start(startInfo);
        }
        catch (Exception) { }
    }

    private static void WriteResult(string resultPath, bool succeeded, string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(resultPath)!);
            var temporaryPath = $"{resultPath}.{Guid.NewGuid():N}.tmp";
            File.WriteAllLines(temporaryPath, [succeeded ? "SUCCESS" : "FAILURE", message]);
            File.Move(temporaryPath, resultPath, true);
        }
        catch (Exception) { }
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
