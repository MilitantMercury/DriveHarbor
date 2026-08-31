using System.Security;
using System.IO;
using Microsoft.Win32;

namespace DriveHarbor.App.Services;

public sealed class WindowsStartupRegistrationService : IStartupRegistrationService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DriveHarbor";

    public bool TrySetEnabled(bool enabled, out string? errorMessage)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            if (enabled)
            {
                var executablePath = Environment.ProcessPath
                    ?? throw new InvalidOperationException("Percorso dell'eseguibile non disponibile.");
                key.SetValue(ValueName, $"\"{executablePath}\" --background", RegistryValueKind.String);
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }

            errorMessage = null;
            return true;
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or SecurityException or IOException or InvalidOperationException)
        {
            errorMessage = $"Windows non ha consentito di modificare l'avvio automatico. {exception.Message}";
            return false;
        }
    }
}
