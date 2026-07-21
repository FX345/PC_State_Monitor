namespace PcGuardianLite.Core;

public sealed record SystemSnapshot(
    string CpuText,
    string MemoryText,
    string DownloadText,
    string UploadText,
    string DiskText,
    string TemperatureText,
    DateTimeOffset CapturedAt);
