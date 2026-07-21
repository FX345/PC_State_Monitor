namespace PcGuardianLite.Core;

public sealed record SpeedTestResult(
    long DownloadedBytes,
    long UploadedBytes,
    TimeSpan DownloadElapsed,
    TimeSpan UploadElapsed)
{
    public double DownloadBytesPerSecond => CalculateBytesPerSecond(DownloadedBytes, DownloadElapsed);

    public double UploadBytesPerSecond => CalculateBytesPerSecond(UploadedBytes, UploadElapsed);

    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.Now;

    private static double CalculateBytesPerSecond(long bytes, TimeSpan elapsed)
    {
        return elapsed.TotalSeconds <= 0 ? 0 : bytes / elapsed.TotalSeconds;
    }
}
