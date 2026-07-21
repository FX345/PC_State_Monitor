namespace PcGuardianLite.Core;

public static class StatusColorPalette
{
    public static string GetHex(MetricStatus status)
    {
        return status switch
        {
            MetricStatus.Normal => "#22C55E",
            MetricStatus.Warning => "#F59E0B",
            MetricStatus.Critical => "#EF4444",
            _ => "#22C55E"
        };
    }
}
