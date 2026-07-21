namespace PcGuardianLite.Core;

public static class SpeedTestFormatter
{
    public static string FormatMegabitsPerSecond(double bytesPerSecond)
    {
        var safeBytesPerSecond = Math.Max(0, bytesPerSecond);
        var megabitsPerSecond = safeBytesPerSecond * 8 / 1_000_000;

        return $"{megabitsPerSecond:0.0} Mbps";
    }
}
