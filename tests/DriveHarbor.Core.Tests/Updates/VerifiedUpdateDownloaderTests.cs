using System.Net;
using System.Security.Cryptography;
using DriveHarbor.Core.Tests.Infrastructure;
using DriveHarbor.Core.Updates;

namespace DriveHarbor.Core.Tests.Updates;

public sealed class VerifiedUpdateDownloaderTests
{
    [Fact]
    public async Task ValidChecksumPublishesPackageAtomically()
    {
        using var directory = new TemporaryDirectory();
        var package = "verified package"u8.ToArray();
        var hash = Convert.ToHexString(SHA256.HashData(package)).ToLowerInvariant();
        using var client = CreateClient(package, $"{hash}  DriveHarbor-v1.1.0-win-x64.zip");
        var downloader = new VerifiedUpdateDownloader(client, directory.FullPath);
        var reports = new List<UpdateDownloadProgress>();

        var result = await downloader.DownloadAsync(CreateUpdate(), new InlineProgress<UpdateDownloadProgress>(reports.Add));

        Assert.True(result.Succeeded);
        Assert.Equal(package, await File.ReadAllBytesAsync(result.PackagePath!));
        Assert.Empty(Directory.EnumerateFiles(directory.FullPath, "*.tmp"));
        var finalProgress = Assert.Single(reports);
        Assert.Equal(package.Length, finalProgress.BytesReceived);
        Assert.Equal(100, finalProgress.Percentage);
    }

    [Fact]
    public async Task InvalidChecksumRejectsAndDeletesTemporaryPackage()
    {
        using var directory = new TemporaryDirectory();
        using var client = CreateClient("tampered"u8.ToArray(), new string('0', 64));
        var downloader = new VerifiedUpdateDownloader(client, directory.FullPath);

        var result = await downloader.DownloadAsync(CreateUpdate());

        Assert.False(result.Succeeded);
        Assert.Empty(Directory.EnumerateFiles(directory.FullPath));
    }

    private static UpdateCheckResult CreateUpdate() => new(
        true,
        "1.1.0",
        new("https://github.com/MilitantMercury/DriveHarbor/releases/tag/v1.1.0"),
        new("https://github.com/package.zip"),
        new("https://github.com/package.zip.sha256"));

    private static HttpClient CreateClient(byte[] package, string checksum) => new(new StubHandler(request =>
        request.RequestUri!.AbsolutePath.EndsWith(".sha256", StringComparison.Ordinal)
            ? new(HttpStatusCode.OK) { Content = new StringContent(checksum) }
            : new(HttpStatusCode.OK) { Content = new ByteArrayContent(package) }));

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }

    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}
