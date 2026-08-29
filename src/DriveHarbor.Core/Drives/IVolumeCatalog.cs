namespace DriveHarbor.Core.Drives;

public interface IVolumeCatalog
{
    IReadOnlyList<VolumeDescriptor> GetAvailableVolumes();
}
