namespace PcGuardianLite.Core;

public sealed record NetworkCounterSample(
    long BytesReceived,
    long BytesSent,
    DateTimeOffset CapturedAt);
