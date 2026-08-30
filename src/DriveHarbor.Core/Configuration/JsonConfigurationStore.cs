using System.Text.Json;
using System.Text.Json.Serialization;

namespace DriveHarbor.Core.Configuration;

public sealed class JsonConfigurationStore : IConfigurationStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string settingsFilePath;

    public JsonConfigurationStore(string? settingsFilePath = null)
    {
        this.settingsFilePath = settingsFilePath ?? AppPaths.DefaultSettingsFile;
    }

    public async Task<ConfigurationLoadResult> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(settingsFilePath))
        {
            return new(AppSettings.CreateDefault(), ConfigurationLoadStatus.DefaultsUsed);
        }

        try
        {
            await using var stream = new FileStream(
                settingsFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);

            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(
                stream,
                SerializerOptions,
                cancellationToken).ConfigureAwait(false);

            if (settings is null)
            {
                return InvalidFileResult();
            }

            if (settings.SchemaVersion != AppSettings.CurrentSchemaVersion)
            {
                return new(
                    AppSettings.CreateDefault(),
                    ConfigurationLoadStatus.UnsupportedVersion,
                    "La configurazione è stata creata da una versione non supportata. Verifica le impostazioni.");
            }

            if (!IsStructurallyValid(settings))
            {
                return InvalidFileResult();
            }

            return new(settings, ConfigurationLoadStatus.Loaded);
        }
        catch (JsonException)
        {
            return InvalidFileResult();
        }
        catch (IOException)
        {
            return new(
                AppSettings.CreateDefault(),
                ConfigurationLoadStatus.InvalidFile,
                "Non è stato possibile leggere la configurazione. Verifica le impostazioni.");
        }
        catch (UnauthorizedAccessException)
        {
            return new(
                AppSettings.CreateDefault(),
                ConfigurationLoadStatus.InvalidFile,
                "Accesso alla configurazione negato. Verifica i permessi della cartella locale.");
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.SchemaVersion != AppSettings.CurrentSchemaVersion)
        {
            throw new ArgumentException("Unsupported configuration schema version.", nameof(settings));
        }

        if (!IsStructurallyValid(settings))
        {
            throw new ArgumentException("The configuration contains invalid values.", nameof(settings));
        }

        var directory = Path.GetDirectoryName(settingsFilePath)
            ?? throw new InvalidOperationException("The settings path must include a directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(settingsFilePath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    settings,
                    SerializerOptions,
                    cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            File.Move(temporaryPath, settingsFilePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static ConfigurationLoadResult InvalidFileResult() => new(
        AppSettings.CreateDefault(),
        ConfigurationLoadStatus.InvalidFile,
        "La configurazione non è valida. Sono stati caricati valori sicuri senza modificare il file originale.");

    private static bool IsStructurallyValid(AppSettings settings) =>
        Enum.IsDefined(settings.Mode)
        && Enum.IsDefined(settings.Theme)
        && settings.Exclusions is not null
        && settings.Exclusions.All(exclusion => !string.IsNullOrWhiteSpace(exclusion))
        && !string.IsNullOrWhiteSpace(settings.LogDirectory);
}
