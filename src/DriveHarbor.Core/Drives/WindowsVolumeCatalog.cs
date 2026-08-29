using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DriveHarbor.Core.Drives;

public sealed class WindowsVolumeCatalog : IVolumeCatalog
{
    public IReadOnlyList<VolumeDescriptor> GetAvailableVolumes()
    {
        if (!OperatingSystem.IsWindows())
        {
            return [];
        }

        var volumes = new List<VolumeDescriptor>();
        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                if (!drive.IsReady)
                {
                    continue;
                }

                var rootPath = EnsureTrailingSeparator(drive.RootDirectory.FullName);
                volumes.Add(new(
                    rootPath,
                    TryGetVolumeGuidPath(rootPath),
                    TryGetVolumeSerialNumber(rootPath),
                    NullIfWhiteSpace(drive.VolumeLabel),
                    drive.DriveType));
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException or Win32Exception)
            {
                // A volume can disappear while Windows enumerates it; omit it from this snapshot.
            }
        }

        return volumes;
    }

    private static string? TryGetVolumeGuidPath(string rootPath)
    {
        var buffer = new char[64];
        if (!GetVolumeNameForVolumeMountPoint(rootPath, buffer, buffer.Length))
        {
            return null;
        }

        var terminatorIndex = Array.IndexOf(buffer, '\0');
        return new string(buffer, 0, terminatorIndex >= 0 ? terminatorIndex : buffer.Length);
    }

    private static string? TryGetVolumeSerialNumber(string rootPath)
    {
        if (!GetVolumeInformation(
            rootPath,
            IntPtr.Zero,
            0,
            out var serialNumber,
            out _,
            out _,
            IntPtr.Zero,
            0))
        {
            return null;
        }

        return $"{serialNumber >> 16:X4}-{serialNumber & 0xFFFF:X4}";
    }

    private static string EnsureTrailingSeparator(string path) =>
        Path.EndsInDirectorySeparator(path) ? path : path + Path.DirectorySeparatorChar;

    private static string? NullIfWhiteSpace(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

#pragma warning disable SYSLIB1054 // StringBuilder interop is clearer and bounded for these Win32 APIs.
    [DllImport("kernel32.dll", EntryPoint = "GetVolumeNameForVolumeMountPointW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetVolumeNameForVolumeMountPoint(
        string volumeMountPoint,
        [Out] char[] volumeName,
        int bufferLength);

    [DllImport("kernel32.dll", EntryPoint = "GetVolumeInformationW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetVolumeInformation(
        string rootPath,
        IntPtr volumeName,
        int volumeNameSize,
        out uint volumeSerialNumber,
        out uint maximumComponentLength,
        out uint fileSystemFlags,
        IntPtr fileSystemName,
        int fileSystemNameSize);
#pragma warning restore SYSLIB1054
}
