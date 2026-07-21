using System.Diagnostics;
using System.Net.Http;

namespace PcGuardianLite.Core;

public sealed class SpeedTestService
{
    private const string DefaultBaseAddress = "https://speed.cloudflare.com";
    private readonly HttpClient httpClient;
    private readonly int downloadBytes;
    private readonly int uploadBytes;

    public SpeedTestService()
        : this(new HttpClient { BaseAddress = new Uri(DefaultBaseAddress), Timeout = TimeSpan.FromSeconds(30) })
    {
    }

    public SpeedTestService(HttpClient httpClient, int downloadBytes = 5 * 1024 * 1024, int uploadBytes = 2 * 1024 * 1024)
    {
        this.httpClient = httpClient;
        this.downloadBytes = Math.Max(1, downloadBytes);
        this.uploadBytes = Math.Max(1, uploadBytes);
    }

    public async Task<SpeedTestResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var (downloadedBytes, downloadElapsed) = await MeasureDownloadAsync(cancellationToken).ConfigureAwait(false);
        var (uploadedBytes, uploadElapsed) = await MeasureUploadAsync(cancellationToken).ConfigureAwait(false);

        return new SpeedTestResult(downloadedBytes, uploadedBytes, downloadElapsed, uploadElapsed)
        {
            CompletedAt = DateTimeOffset.Now
        };
    }

    private async Task<(long Bytes, TimeSpan Elapsed)> MeasureDownloadAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        using var response = await httpClient
            .GetAsync($"/__down?bytes={downloadBytes}", HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var buffer = new byte[64 * 1024];
        long totalBytes = 0;

        while (true)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            totalBytes += read;
        }

        stopwatch.Stop();
        return (totalBytes, stopwatch.Elapsed);
    }

    private async Task<(long Bytes, TimeSpan Elapsed)> MeasureUploadAsync(CancellationToken cancellationToken)
    {
        var payload = new byte[uploadBytes];
        Array.Fill<byte>(payload, 0x5A);

        using var content = new ByteArrayContent(payload);
        var stopwatch = Stopwatch.StartNew();
        using var response = await httpClient.PostAsync("/__up", content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        stopwatch.Stop();

        return (payload.LongLength, stopwatch.Elapsed);
    }
}
