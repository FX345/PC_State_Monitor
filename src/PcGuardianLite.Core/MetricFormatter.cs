namespace PcGuardianLite.Core;

public static class MetricFormatter
{
    public static string FormatPercent(double value)
    {
        var clamped = Math.Clamp(value, 0, 100);

        if (Math.Abs(clamped - Math.Round(clamped)) < 0.05)
        {
            return $"{Math.Round(clamped):0}%";
        }

        return $"{clamped:0.0}%";
    }

    public static string FormatBytesPerSecond(double bytesPerSecond)
    {
        var safeValue = Math.Max(0, bytesPerSecond);

        if (safeValue < 1024)
        {
            return $"{safeValue:0} B/s";
        }

        if (safeValue < 1024 * 1024)
        {
            return $"{safeValue / 1024:0.0} KB/s";
        }

        return $"{safeValue / 1024 / 1024:0.0} MB/s";
    }

    public static string FormatTemperature(double? celsius)
    {
        return celsius.HasValue ? $"{celsius.Value:0.#} °C" : "Not supported";
    }
}
