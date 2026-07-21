namespace PcGuardianLite.Core;

public static class HealthScoreCalculator
{
    public static int Calculate(double cpuPercent, double memoryPercent, double diskPercent)
    {
        return Calculate(cpuPercent, memoryPercent, diskPercent, downloadBytesPerSecond: 0, uploadBytesPerSecond: 0);
    }

    public static int Calculate(
        double cpuPercent,
        double memoryPercent,
        double diskPercent,
        double downloadBytesPerSecond,
        double uploadBytesPerSecond)
    {
        var networkMbPerSecond = Math.Max(0, downloadBytesPerSecond + uploadBytesPerSecond) / 1024d / 1024d;
        var penalty =
            PressurePenalty(cpuPercent, warningThreshold: 50, criticalThreshold: 90, warningPenalty: 5, criticalPenalty: 25) +
            PressurePenalty(memoryPercent, warningThreshold: 60, criticalThreshold: 90, warningPenalty: 5, criticalPenalty: 30) +
            PressurePenalty(diskPercent, warningThreshold: 75, criticalThreshold: 95, warningPenalty: 5, criticalPenalty: 25) +
            PressurePenalty(networkMbPerSecond, warningThreshold: 20, criticalThreshold: 80, warningPenalty: 0, criticalPenalty: 5);

        return (int)Math.Clamp(Math.Round(100 - penalty), 0, 100);
    }

    private static double PressurePenalty(
        double value,
        double warningThreshold,
        double criticalThreshold,
        double warningPenalty,
        double criticalPenalty)
    {
        var safeValue = Math.Clamp(value, 0, double.MaxValue);

        if (safeValue <= warningThreshold)
        {
            return warningThreshold <= 0
                ? 0
                : safeValue / warningThreshold * warningPenalty;
        }

        if (safeValue <= criticalThreshold)
        {
            var pressureRange = criticalThreshold - warningThreshold;
            var penaltyRange = criticalPenalty - warningPenalty;
            return warningPenalty + ((safeValue - warningThreshold) / pressureRange * penaltyRange);
        }

        return criticalPenalty;
    }
}
