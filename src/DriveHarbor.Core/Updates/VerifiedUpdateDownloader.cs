using System.Security.Cryptography;

namespace DriveHarbor.Core.Updates;

public sealed class VerifiedUpdateDownloader(HttpClient httpClient, string updatesDirectory) : IUpdateDownloader
{
    private const long MaximumPackageBytes = 250 * 1024 * 1024;

    public async Task<UpdateDownloadResult> DownloadAsync(UpdateCheckResult update, CancellationToken cancellationToken = default)
    {
        if (!update.IsAvailable || update.Version is null || update.PackageUri is null || update.ChecksumUri is null)
            return new(false, "La release non contiene pacchetto e checksum richiesti.");
        Directory.CreateDirectory(updatesDirectory);
        var packagePath = Path.Combine(updatesDirectory, $"DriveHarbor-v{update.Version}-win-x64.zip");
        var temporaryPath = $"{packagePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            var checksumText = await httpClient.GetStringAsync(update.ChecksumUri, cancellationToken).ConfigureAwait(false);
            var expectedHash = checksumText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (expectedHash is null || expectedHash.Length != 64 || !expectedHash.All(Uri.IsHexDigit))
                return new(false, "Il checksum pubblicato non è valido.");
            using var response = await httpClient.GetAsync(update.PackageUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength > MaximumPackageBytes)
                return new(false, "Il pacchetto supera la dimensione massima consentita.");
            await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
            await using (var destination = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
            {
                await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
                if (destination.Length > MaximumPackageBytes) return new(false, "Il pacchetto supera la dimensione massima consentita.");
            }
            var actualHash = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(temporaryPath, cancellationToken))).ToLowerInvariant();
            if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(expectedHash), Convert.FromHexString(actualHash)))
                return new(false, "Verifica SHA-256 non riuscita. Il pacchetto è stato rifiutato.");
            File.Move(temporaryPath, packagePath, true);
            return new(true, "Aggiornamento scaricato e verificato.", packagePath);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
}
