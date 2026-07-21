namespace PcGuardianLite.Core;

public sealed record NetworkSpeed(
    double DownloadBytesPerSecond,
    double UploadBytesPerSecond);
