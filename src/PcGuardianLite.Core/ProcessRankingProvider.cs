using System.Diagnostics;

namespace PcGuardianLite.Core;

public static class ProcessRankingProvider
{
    public static IEnumerable<ProcessSnapshot> GetTopProcesses(int top)
    {
        var snapshots = new List<ProcessSnapshot>();

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                snapshots.Add(new ProcessSnapshot(
                    process.ProcessName,
                    process.Id,
                    Math.Round(process.WorkingSet64 / 1024d / 1024d, 1),
                    Math.Round(process.TotalProcessorTime.TotalSeconds, 1)));
            }
            catch
            {
            }
            finally
            {
                process.Dispose();
            }
        }

        return snapshots
            .OrderByDescending(process => process.MemoryMb)
            .ThenByDescending(process => process.CpuSeconds)
            .Take(top);
    }
}
