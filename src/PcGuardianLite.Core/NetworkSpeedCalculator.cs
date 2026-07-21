namespace PcGuardianLite.Core;

public static class NetworkSpeedCalculator
{
    public static NetworkSpeed Calculate(NetworkCounterSample previous, NetworkCounterSample current)
    {
        var elapsedSeconds = (current.CapturedAt - previous.CapturedAt).TotalSeconds;

        if (elapsedSeconds <= 0)
        {
            return new NetworkSpeed(0, 0);
        }

        var receivedDelta = Math.Max(0, current.BytesReceived - previous.BytesReceived);
        var sentDelta = Math.Max(0, current.BytesSent - previous.BytesSent);

        return new NetworkSpeed(
            receivedDelta / elapsedSeconds,
            sentDelta / elapsedSeconds);
    }
}
