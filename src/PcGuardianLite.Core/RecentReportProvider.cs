namespace PcGuardianLite.Core;

public static class RecentReportProvider
{
    public static IEnumerable<ReportFileInfo> GetRecentReports(string reportDirectory, int top)
    {
        if (!Directory.Exists(reportDirectory))
        {
            return Array.Empty<ReportFileInfo>();
        }

        return Directory
            .EnumerateFiles(reportDirectory, "*.html")
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .Take(top)
            .Select(file => new ReportFileInfo(file.Name, file.FullName, file.LastWriteTime));
    }
}
