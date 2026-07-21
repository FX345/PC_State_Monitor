namespace PcGuardianLite.Core;

public sealed record CleanupResult(int DeletedCount, int SkippedCount, long FreedBytes);
