namespace PcGuardianLite.Core;

public static class FloatingBallTextFormatter
{
    public static string FormatMemory(string memoryText)
    {
        return $"内存 {memoryText}";
    }

    public static string FormatDownload(string downloadText)
    {
        return $"下载 {downloadText}";
    }
}
