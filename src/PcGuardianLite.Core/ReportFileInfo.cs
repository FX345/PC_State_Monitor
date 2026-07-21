namespace PcGuardianLite.Core;

public sealed record ReportFileInfo(
    string Name,
    string FullPath,
    DateTime LastWriteTime);
