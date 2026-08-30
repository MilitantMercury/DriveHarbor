using System.Diagnostics;
using System.IO;

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
        var startInfo = new ProcessStartInfo(updaterCopy) { UseShellExecute = false, CreateNoWindow = true };
        startInfo.ArgumentList.Add(Path.GetFullPath(packagePath));
        startInfo.ArgumentList.Add(installDirectory);
        startInfo.ArgumentList.Add(Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        startInfo.ArgumentList.Add(appPath);
        Process.Start(startInfo);
        errorMessage = null;
        return true;
    }
}
