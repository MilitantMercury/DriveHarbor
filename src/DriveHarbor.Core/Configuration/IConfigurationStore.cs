namespace DriveHarbor.Core.Configuration;

public interface IConfigurationStore
{
    Task<ConfigurationLoadResult> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
