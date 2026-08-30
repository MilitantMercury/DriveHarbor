using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace DriveHarbor.Core.Updates;

public sealed class GitHubUpdateChecker(HttpClient httpClient) : IUpdateChecker
{
    private static readonly Uri ReleasesUri = new("https://api.github.com/repos/MilitantMercury/DriveHarbor/releases?per_page=20");

    public async Task<UpdateCheckResult> CheckAsync(string currentVersion, UpdateChannel channel, CancellationToken cancellationToken = default)
    {
        if (!SemanticVersion.TryParse(currentVersion, out var current))
            throw new ArgumentException("Invalid current semantic version.", nameof(currentVersion));
        using var request = new HttpRequestMessage(HttpMethod.Get, ReleasesUri);
        request.Headers.UserAgent.ParseAdd("DriveHarbor-UpdateChecker/1.0");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var releases = await response.Content.ReadFromJsonAsync<Release[]>(cancellationToken: cancellationToken).ConfigureAwait(false) ?? [];
        var candidate = releases
            .Where(release => !release.Draft && (channel == UpdateChannel.Beta || !release.Prerelease))
            .Select(release => new { Release = release, Parsed = Parse(release.TagName) })
            .Where(item => item.Parsed is not null && item.Parsed.CompareTo(current) > 0)
            .OrderByDescending(item => item.Parsed)
            .FirstOrDefault();
        if (candidate is null) return new(false);
        var prefix = $"DriveHarbor-v{candidate.Parsed}-win-x64.zip";
        var package = FindAsset(candidate.Release, prefix);
        var checksum = FindAsset(candidate.Release, $"{prefix}.sha256");
        var attestation = FindAsset(candidate.Release, $"{prefix}.sigstore.json");
        return new(true, candidate.Parsed!.ToString(), new(candidate.Release.HtmlUrl), package, checksum, attestation);
    }

    private static SemanticVersion? Parse(string value) => SemanticVersion.TryParse(value, out var parsed) ? parsed : null;

    private static Uri? FindAsset(Release release, string name)
    {
        var url = release.Assets.FirstOrDefault(asset => string.Equals(asset.Name, name, StringComparison.Ordinal))?.DownloadUrl;
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == "https" && uri.Host == "github.com" ? uri : null;
    }

    private sealed record Release(
        [property: JsonPropertyName("tag_name")] string TagName,
        [property: JsonPropertyName("html_url")] string HtmlUrl,
        [property: JsonPropertyName("draft")] bool Draft,
        [property: JsonPropertyName("prerelease")] bool Prerelease,
        [property: JsonPropertyName("assets")] ReleaseAsset[] Assets);

    private sealed record ReleaseAsset(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("browser_download_url")] string DownloadUrl);
}
