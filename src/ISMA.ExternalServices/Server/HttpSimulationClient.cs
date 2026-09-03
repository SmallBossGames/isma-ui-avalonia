using System.Collections.Immutable;
using System.Net.Http.Json;
using ISMA.Domain.Dtos;
using Microsoft.Extensions.Logging;

namespace ISMA.ExternalServices.Server;

public sealed class HttpSimulationClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger? _logger;
    private readonly string _cacheDirectory;

    public HttpSimulationClient(IUnixSocketHandler socketHandler, string httpAddress, ILogger? logger = null)
    {
        _httpClient = socketHandler.CreateHttpClient(httpAddress);
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
        _logger = logger;

        _cacheDirectory = Path.Combine(Path.GetTempPath(), "isma-simulation-cache");
        if (!Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
        }
    }

    public HttpSimulationClient(HttpClient httpClient, ILogger? logger = null)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
        _logger = logger;

        _cacheDirectory = Path.Combine(Path.GetTempPath(), "isma-simulation-cache");
        if (!Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
        }
    }

    public async Task<FileInfo> DownloadResultToFileAsync(string downloadUrl, string destinationPath, CancellationToken ct = default)
    {
        _logger?.LogInformation("Downloading simulation result from {Url} to {Path}", downloadUrl, destinationPath);

        var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var dir = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await using var stream = File.Create(destinationPath);
        await using var downloadStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        await downloadStream.CopyToAsync(stream, ct).ConfigureAwait(false);

        _logger?.LogInformation("Downloaded simulation result to {Path}", destinationPath);
        return new FileInfo(destinationPath);
    }

    public FileInfo DownloadResultToFile(string downloadUrl, string destinationPath, CancellationToken ct = default)
    {
        return DownloadResultToFileAsync(downloadUrl, destinationPath, ct).GetAwaiter().GetResult();
    }

    public async Task<FileInfo> DownloadToCacheAsync(string downloadUrl, CancellationToken ct = default)
    {
        var fileName = Path.GetFileName(new Uri(downloadUrl).AbsolutePath) ?? Guid.NewGuid().ToString();
        var cachePath = Path.Combine(_cacheDirectory, fileName);
        return await DownloadResultToFileAsync(downloadUrl, cachePath, ct).ConfigureAwait(false);
    }

    public FileInfo DownloadToCache(string downloadUrl, CancellationToken ct = default)
    {
        return DownloadToCacheAsync(downloadUrl, ct).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
