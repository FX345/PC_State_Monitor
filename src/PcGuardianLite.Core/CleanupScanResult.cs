namespace PcGuardianLite.Core;

public sealed record CleanupScanResult(IReadOnlyList<CleanupTarget> Items, int SkippedCount)
{
    public long TotalBytes => Items.Sum(item => item.SizeBytes);
}
