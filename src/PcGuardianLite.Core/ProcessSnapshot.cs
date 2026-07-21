namespace PcGuardianLite.Core;

public sealed record ProcessSnapshot(
    string Name,
    int Id,
    double MemoryMb,
    double CpuSeconds);
