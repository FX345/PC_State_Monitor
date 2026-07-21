namespace PcGuardianLite.Core;

public static class FloatingBallMenuTextFormatter
{
    public static string FormatPanelToggle(bool isPanelOpen)
    {
        return isPanelOpen ? "关闭面板" : "打开面板";
    }

    public static string FormatMonitorToggle(bool isMonitoring)
    {
        return isMonitoring ? "暂停监控" : "继续监控";
    }
}
