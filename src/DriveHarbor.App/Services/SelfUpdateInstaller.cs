using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using DriveHarbor.Core.Configuration;

namespace DriveHarbor.App.Services;

public sealed class SelfUpdateInstaller : IUpdateInstaller
{
    public bool TryStart(string packagePath, out string? errorMessage)
    {
        var installDirectory = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
        var updaterSource = Path.Combine(installDirectory, "DriveHarbor.Updater.exe");
        var appPath = Environment.ProcessPath;
        if (!File.Exists(updaterSource) || string.IsNullOrWhiteSpace(appPath))
        {
            errorMessage = "Il componente di aggiornamento non è presente in questa installazione.";
            return false;
        }
        var updaterCopy = Path.Combine(Path.GetTempPath(), $"DriveHarbor.Updater.{Guid.NewGuid():N}.exe");
        File.Copy(updaterSource, updaterCopy);
        Directory.CreateDirectory(AppPaths.UpdatesDirectory);
        File.Delete(AppPaths.UpdateResultFile);
        var requiresElevation = !CanWriteToDirectory(installDirectory);
        var startInfo = new ProcessStartInfo(updaterCopy)
        {
            UseShellExecute = requiresElevation,
            CreateNoWindow = !requiresElevation,
            Verb = requiresElevation ? "runas" : string.Empty,
        };
        startInfo.ArgumentList.Add(Path.GetFullPath(packagePath));
        startInfo.ArgumentList.Add(installDirectory);
        startInfo.ArgumentList.Add(Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        startInfo.ArgumentList.Add(appPath);
        try
        {
            Process.Start(startInfo);
        }
        catch (Win32Exception exception) when (exception.NativeErrorCode == 1223)
        {
            errorMessage = "Autorizzazione amministratore annullata. DriveHarbor è rimasto aperto e non è stato modificato.";
            return false;
        }
        errorMessage = null;
        return true;
    }

    private static bool CanWriteToDirectory(string directory)
    {
        var probe = Path.Combine(directory, $".driveharbor-write-{Guid.NewGuid():N}.tmp");
        try
        {
            using (File.Create(probe, 1, FileOptions.DeleteOnClose)) { }
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }
}
