namespace PcGuardianLite.Core;

public sealed class SystemMonitorService
{
    private readonly IMetricProvider metricProvider;
    private NetworkCounterSample? previousNetworkSample;

    public SystemMonitorService(IMetricProvider metricProvider)
    {
        this.metricProvider = metricProvider;
    }

    public SystemSnapshot CaptureSnapshot()
    {
        var currentNetworkSample = metricProvider.GetNetworkSample();
        var networkSpeed = previousNetworkSample is null
            ? new NetworkSpeed(0, 0)
            : NetworkSpeedCalculator.Calculate(previousNetworkSample, currentNetworkSample);

        previousNetworkSample = currentNetworkSample;

        return new SystemSnapshot(
            MetricFormatter.FormatPercent(metricProvider.GetCpuUsagePercent()),
            MetricFormatter.FormatPercent(metricProvider.GetMemoryUsagePercent()),
            MetricFormatter.FormatBytesPerSecond(networkSpeed.DownloadBytesPerSecond),
            MetricFormatter.FormatBytesPerSecond(networkSpeed.UploadBytesPerSecond),
            MetricFormatter.FormatPercent(metricProvider.GetDiskUsagePercent()),
            MetricFormatter.FormatTemperature(metricProvider.GetTemperatureCelsius()),
            DateTimeOffset.Now);
    }
}
