namespace PcGuardianLite.Core;

public static class MetricStatusCalculator
{
    public static MetricStatus FromPercent(double value, double warningThreshold, double criticalThreshold)
    {
        if (value >= criticalThreshold)
        {
            return MetricStatus.Critical;
        }

        if (value >= warningThreshold)
        {
            return MetricStatus.Warning;
        }

        return MetricStatus.Normal;
    }
}
