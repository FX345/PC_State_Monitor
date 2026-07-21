namespace PcGuardianLite.Core;

public sealed record CleanupTarget(
    string Id,
    CleanupTargetKind Kind,
    string DisplayName,
    string Path,
    long SizeBytes,
    bool IsSelected);
