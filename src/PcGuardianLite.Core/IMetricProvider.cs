namespace PcGuardianLite.Core;

public interface IMetricProvider
{
    double GetCpuUsagePercent();

    double GetMemoryUsagePercent();

    NetworkCounterSample GetNetworkSample();

    double GetDiskUsagePercent();

    double? GetTemperatureCelsius();
}
