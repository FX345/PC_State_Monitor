using System.Diagnostics;
using PcGuardianLite.Core;

var tests = new List<(string Name, Action Run)>
{
    ("FormatPercent clamps low values", () => AssertEqual("0%", MetricFormatter.FormatPercent(-4.2))),
    ("FormatPercent clamps high values", () => AssertEqual("100%", MetricFormatter.FormatPercent(140.8))),
    ("FormatPercent rounds values", () => AssertEqual("42.6%", MetricFormatter.FormatPercent(42.56))),
    ("FormatBytesPerSecond uses B/s", () => AssertEqual("512 B/s", MetricFormatter.FormatBytesPerSecond(512))),
    ("FormatBytesPerSecond uses KB/s", () => AssertEqual("2.0 KB/s", MetricFormatter.FormatBytesPerSecond(2048))),
    ("FormatBytesPerSecond uses MB/s", () => AssertEqual("3.0 MB/s", MetricFormatter.FormatBytesPerSecond(3 * 1024 * 1024))),
    ("FormatTemperature reports unsupported", () => AssertEqual("Not supported", MetricFormatter.FormatTemperature(null))),
    ("Floating ball memory label uses Chinese text", () => AssertEqual("内存 42.6%", FloatingBallTextFormatter.FormatMemory("42.6%"))),
    ("Floating ball download label uses Chinese text", () => AssertEqual("下载 2.0 KB/s", FloatingBallTextFormatter.FormatDownload("2.0 KB/s"))),
    ("Context menu shows close panel when panel is open", TestContextMenuClosePanelText),
    ("Context menu shows open panel when panel is closed", TestContextMenuOpenPanelText),
    ("Context menu shows pause monitor when monitor is running", TestContextMenuPauseMonitorText),
    ("Context menu shows resume monitor when monitor is paused", TestContextMenuResumeMonitorText),
    ("Tray menu shows hide action", () => AssertEqual("隐藏到托盘", TrayMenuTextFormatter.HideToTrayText)),
    ("Tray menu shows show action", () => AssertEqual("显示悬浮球", TrayMenuTextFormatter.ShowFloatingBallText)),
    ("Metric status returns normal below warning threshold", () => AssertEqual(MetricStatus.Normal, MetricStatusCalculator.FromPercent(45, warningThreshold: 70, criticalThreshold: 90))),
    ("Metric status returns warning above warning threshold", () => AssertEqual(MetricStatus.Warning, MetricStatusCalculator.FromPercent(76, warningThreshold: 70, criticalThreshold: 90))),
    ("Metric status returns critical above critical threshold", () => AssertEqual(MetricStatus.Critical, MetricStatusCalculator.FromPercent(95, warningThreshold: 70, criticalThreshold: 90))),
    ("Status color returns green for normal", () => AssertEqual("#22C55E", StatusColorPalette.GetHex(MetricStatus.Normal))),
    ("Health score uses threshold weighted pressure", () => AssertEqual(63, HealthScoreCalculator.Calculate(cpuPercent: 80, memoryPercent: 70, diskPercent: 60))),
    ("Health score penalizes critical disk pressure", TestHealthScorePenalizesCriticalDisk),
    ("Snap calculator attaches to left edge", TestSnapCalculatorAttachesLeft),
    ("Snap calculator leaves centered position alone", TestSnapCalculatorLeavesCenterAlone),
    ("Recent reports returns newest files first", TestRecentReportsNewestFirst),
    ("Drag calculator applies mouse movement", TestDragCalculatorAppliesMovement),
    ("Network delta calculates receive speed", TestReceiveSpeed),
    ("Network delta calculates send speed", TestSendSpeed),
    ("Network delta clamps invalid elapsed time", TestInvalidElapsedTime),
    ("System monitor formats snapshot", TestSystemMonitorFormatsSnapshot),
    ("Script launcher rejects missing script", TestScriptLauncherRejectsMissingScript),
    ("Script launcher builds safe PowerShell arguments", TestScriptLauncherBuildsSafeArguments),
    ("Installer planner uses local app data", TestInstallerPlannerUsesLocalAppData),
    ("Installer planner builds user desktop shortcut path", TestInstallerPlannerBuildsDesktopShortcutPath),
    ("Installer payload manifest requires bundled scripts", TestInstallerPayloadRequiresBundledScripts),
    ("Single instance guard blocks second owner", TestSingleInstanceGuardBlocksSecondOwner),
    ("Single instance guard releases ownership on dispose", TestSingleInstanceGuardReleasesOwnership),
    ("Uninstall planner builds uninstall command", TestUninstallPlannerBuildsUninstallCommand),
    ("Uninstall planner builds uninstall registry path", TestUninstallPlannerBuildsRegistryPath),
    ("Uninstall planner builds uninstall shortcut path", TestUninstallPlannerBuildsShortcutPath),
    ("Safe cleanup scan includes only old temp files", TestSafeCleanupScanIncludesOnlyOldTempFiles),
    ("Safe cleanup skips reparse point directories", TestSafeCleanupSkipsReparsePointDirectories),
    ("Safe cleanup deletes only selected white-listed files", TestSafeCleanupDeletesOnlySelectedWhitelistedFiles),
    ("Safe cleanup ignores inaccessible empty-directory cleanup roots", TestSafeCleanupIgnoresInaccessibleEmptyDirectoryCleanupRoots),
    ("Main window uses cyber HUD skin resources", TestMainWindowUsesCyberHudSkinResources),
    ("Main window uses tabbed tool layout", TestMainWindowUsesTabbedToolLayout)
};

var failed = 0;

foreach (var test in tests)
{
    try
    {
        test.Run();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception ex)
    {
        failed++;
        Console.WriteLine($"FAIL {test.Name}");
        Console.WriteLine(ex.Message);
    }
}

if (failed > 0)
{
    Console.WriteLine($"{failed} test(s) failed.");
    Environment.Exit(1);
}

Console.WriteLine($"{tests.Count} test(s) passed.");

static void TestReceiveSpeed()
{
    var previous = new NetworkCounterSample(1000, 500, new DateTimeOffset(2026, 7, 2, 10, 0, 0, TimeSpan.Zero));
    var current = new NetworkCounterSample(3048, 500, new DateTimeOffset(2026, 7, 2, 10, 0, 1, TimeSpan.Zero));

    var speed = NetworkSpeedCalculator.Calculate(previous, current);

    AssertEqual(2048d, speed.DownloadBytesPerSecond);
    AssertEqual(0d, speed.UploadBytesPerSecond);
}

static void TestContextMenuClosePanelText()
{
    AssertEqual("关闭面板", FloatingBallMenuTextFormatter.FormatPanelToggle(isPanelOpen: true));
}

static void TestContextMenuOpenPanelText()
{
    AssertEqual("打开面板", FloatingBallMenuTextFormatter.FormatPanelToggle(isPanelOpen: false));
}

static void TestContextMenuPauseMonitorText()
{
    AssertEqual("暂停监控", FloatingBallMenuTextFormatter.FormatMonitorToggle(isMonitoring: true));
}

static void TestContextMenuResumeMonitorText()
{
    AssertEqual("继续监控", FloatingBallMenuTextFormatter.FormatMonitorToggle(isMonitoring: false));
}

static void TestDragCalculatorAppliesMovement()
{
    var position = DragPositionCalculator.Calculate(
        windowLeft: 100,
        windowTop: 200,
        startScreenX: 20,
        startScreenY: 30,
        currentScreenX: 35,
        currentScreenY: 55);

    AssertEqual(115d, position.Left);
    AssertEqual(225d, position.Top);
}

static void TestSnapCalculatorAttachesLeft()
{
    var snapped = SnapPositionCalculator.Snap(
        left: 14,
        top: 120,
        width: 104,
        height: 104,
        workAreaLeft: 0,
        workAreaTop: 0,
        workAreaRight: 1920,
        workAreaBottom: 1080,
        threshold: 24);

    AssertEqual(0d, snapped.Left);
    AssertEqual(120d, snapped.Top);
}

static void TestSnapCalculatorLeavesCenterAlone()
{
    var snapped = SnapPositionCalculator.Snap(
        left: 500,
        top: 300,
        width: 104,
        height: 104,
        workAreaLeft: 0,
        workAreaTop: 0,
        workAreaRight: 1920,
        workAreaBottom: 1080,
        threshold: 24);

    AssertEqual(500d, snapped.Left);
    AssertEqual(300d, snapped.Top);
}

static void TestHealthScorePenalizesCriticalDisk()
{
    var normalDiskScore = HealthScoreCalculator.Calculate(cpuPercent: 30, memoryPercent: 40, diskPercent: 40);
    var criticalDiskScore = HealthScoreCalculator.Calculate(cpuPercent: 30, memoryPercent: 40, diskPercent: 96);

    if (criticalDiskScore >= normalDiskScore)
    {
        throw new UnreachableException($"Expected critical disk score below normal disk score, got {criticalDiskScore} and {normalDiskScore}.");
    }

    AssertEqual(69, criticalDiskScore);
}

static void TestRecentReportsNewestFirst()
{
    var directory = Path.Combine(Path.GetTempPath(), $"pc-guardian-reports-{Guid.NewGuid():N}");
    Directory.CreateDirectory(directory);

    try
    {
        var oldReport = Path.Combine(directory, "old.html");
        var newReport = Path.Combine(directory, "new.html");
        File.WriteAllText(oldReport, "old");
        File.WriteAllText(newReport, "new");
        File.SetLastWriteTimeUtc(oldReport, new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc));
        File.SetLastWriteTimeUtc(newReport, new DateTime(2026, 7, 2, 10, 0, 0, DateTimeKind.Utc));

        var reports = RecentReportProvider.GetRecentReports(directory, top: 2).ToArray();

        AssertEqual("new.html", reports[0].Name);
        AssertEqual("old.html", reports[1].Name);
    }
    finally
    {
        Directory.Delete(directory, recursive: true);
    }
}

static void TestSendSpeed()
{
    var previous = new NetworkCounterSample(1000, 500, new DateTimeOffset(2026, 7, 2, 10, 0, 0, TimeSpan.Zero));
    var current = new NetworkCounterSample(1000, 4596, new DateTimeOffset(2026, 7, 2, 10, 0, 2, TimeSpan.Zero));

    var speed = NetworkSpeedCalculator.Calculate(previous, current);

    AssertEqual(0d, speed.DownloadBytesPerSecond);
    AssertEqual(2048d, speed.UploadBytesPerSecond);
}

static void TestInvalidElapsedTime()
{
    var capturedAt = new DateTimeOffset(2026, 7, 2, 10, 0, 0, TimeSpan.Zero);
    var previous = new NetworkCounterSample(1000, 500, capturedAt);
    var current = new NetworkCounterSample(3048, 4596, capturedAt);

    var speed = NetworkSpeedCalculator.Calculate(previous, current);

    AssertEqual(0d, speed.DownloadBytesPerSecond);
    AssertEqual(0d, speed.UploadBytesPerSecond);
}

static void TestSystemMonitorFormatsSnapshot()
{
    var provider = new FakeMetricProvider
    {
        CpuUsagePercent = 17.45,
        MemoryUsagePercent = 66.64,
        DiskUsagePercent = 71.11,
        TemperatureCelsius = null,
        NetworkSample = new NetworkCounterSample(3000, 6000, new DateTimeOffset(2026, 7, 2, 10, 0, 1, TimeSpan.Zero))
    };
    var monitor = new SystemMonitorService(provider);

    _ = monitor.CaptureSnapshot();
    provider.NetworkSample = new NetworkCounterSample(5048, 8048, new DateTimeOffset(2026, 7, 2, 10, 0, 2, TimeSpan.Zero));
    var snapshot = monitor.CaptureSnapshot();

    AssertEqual("17.5%", snapshot.CpuText);
    AssertEqual("66.6%", snapshot.MemoryText);
    AssertEqual("2.0 KB/s", snapshot.DownloadText);
    AssertEqual("2.0 KB/s", snapshot.UploadText);
    AssertEqual("71.1%", snapshot.DiskText);
    AssertEqual("Not supported", snapshot.TemperatureText);
}

static void TestScriptLauncherRejectsMissingScript()
{
    var result = ScriptLauncher.RunScript(@"Z:\definitely-missing-script.ps1");

    AssertEqual(false, result.Started);
    AssertEqual("Script not found.", result.Message);
}

static void TestScriptLauncherBuildsSafeArguments()
{
    var tempFile = Path.Combine(Path.GetTempPath(), $"pc-guardian-test-{Guid.NewGuid():N}.ps1");
    File.WriteAllText(tempFile, "Write-Host test");

    try
    {
        var startInfo = ScriptLauncher.BuildPowerShellStartInfo(tempFile, "-Path D:\\");

        AssertEqual("powershell.exe", startInfo.FileName);
        AssertEqual(false, startInfo.UseShellExecute);
        AssertEqual("-NoProfile", startInfo.ArgumentList[0]);
        AssertEqual("-ExecutionPolicy", startInfo.ArgumentList[1]);
        AssertEqual("Bypass", startInfo.ArgumentList[2]);
        AssertEqual("-File", startInfo.ArgumentList[3]);
        AssertEqual(tempFile, startInfo.ArgumentList[4]);
        AssertEqual("-Path", startInfo.ArgumentList[5]);
        AssertEqual(@"D:\", startInfo.ArgumentList[6]);
    }
    finally
    {
        File.Delete(tempFile);
    }
}

static void TestInstallerPlannerUsesLocalAppData()
{
    var directory = InstallerPathPlanner.GetInstallDirectory(@"C:\Users\Alice\AppData\Local", "PcGuardianLite");

    AssertEqual(@"C:\Users\Alice\AppData\Local\PcGuardianLite", directory);
}

static void TestInstallerPlannerBuildsDesktopShortcutPath()
{
    var shortcutPath = InstallerPathPlanner.GetDesktopShortcutPath(@"C:\Users\Alice\Desktop", "PcGuardianLite");

    AssertEqual(@"C:\Users\Alice\Desktop\PcGuardianLite.lnk", shortcutPath);
}

static void TestInstallerPayloadRequiresBundledScripts()
{
    var directory = Path.Combine(Path.GetTempPath(), $"pc-guardian-payload-{Guid.NewGuid():N}");
    Directory.CreateDirectory(directory);

    try
    {
        File.WriteAllText(Path.Combine(directory, "PcGuardianLite.exe"), "");
        File.WriteAllText(Path.Combine(directory, "pc_report.ps1"), "");
        File.WriteAllText(Path.Combine(directory, "network_report.ps1"), "");
        File.WriteAllText(Path.Combine(directory, "folder_radar.ps1"), "");
        File.WriteAllText(Path.Combine(directory, "ai_review_pack.ps1"), "");
        File.WriteAllText(Path.Combine(directory, "cmd_for_folder_radar.txt"), "");

        AssertEqual(true, InstallerPayloadManifest.HasRequiredFiles(directory));
        File.Delete(Path.Combine(directory, "network_report.ps1"));
        AssertEqual(false, InstallerPayloadManifest.HasRequiredFiles(directory));
    }
    finally
    {
        Directory.Delete(directory, recursive: true);
    }
}

static void TestSingleInstanceGuardBlocksSecondOwner()
{
    var mutexName = $@"Local\PcGuardianLite.Tests.{Guid.NewGuid():N}";
    using var first = SingleInstanceGuard.TryAcquire(mutexName);
    var secondHasOwnership = TryAcquireSingleInstanceOnWorkerThread(mutexName);

    AssertEqual(true, first.HasOwnership);
    AssertEqual(false, secondHasOwnership);
}

static void TestSingleInstanceGuardReleasesOwnership()
{
    var mutexName = $@"Local\PcGuardianLite.Tests.{Guid.NewGuid():N}";
    using (var first = SingleInstanceGuard.TryAcquire(mutexName))
    {
        AssertEqual(true, first.HasOwnership);
    }

    using var second = SingleInstanceGuard.TryAcquire(mutexName);
    AssertEqual(true, second.HasOwnership);
}

static bool TryAcquireSingleInstanceOnWorkerThread(string mutexName)
{
    var hasOwnership = false;
    var thread = new Thread(() =>
    {
        using var guard = SingleInstanceGuard.TryAcquire(mutexName);
        hasOwnership = guard.HasOwnership;
    });

    thread.Start();
    thread.Join();

    return hasOwnership;
}

static void TestUninstallPlannerBuildsUninstallCommand()
{
    var command = UninstallPlanner.GetUninstallCommand(@"C:\Users\Alice\AppData\Local\PcGuardianLite", "PcGuardianLite.exe");

    AssertEqual("\"C:\\Users\\Alice\\AppData\\Local\\PcGuardianLite\\PcGuardianLite.exe\" --uninstall", command);
}

static void TestUninstallPlannerBuildsRegistryPath()
{
    var path = UninstallPlanner.GetCurrentUserUninstallRegistryPath("PcGuardianLite");

    AssertEqual(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\PcGuardianLite", path);
}

static void TestUninstallPlannerBuildsShortcutPath()
{
    var path = UninstallPlanner.GetUninstallShortcutPath(@"C:\Users\Alice\AppData\Local\PcGuardianLite");

    AssertEqual(@"C:\Users\Alice\AppData\Local\PcGuardianLite\Uninstall PcGuardianLite.bat", path);
}

static void TestSafeCleanupScanIncludesOnlyOldTempFiles()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), $"pc-guardian-cleanup-{Guid.NewGuid():N}");
    Directory.CreateDirectory(tempRoot);

    try
    {
        var oldFile = Path.Combine(tempRoot, "old.tmp");
        var newFile = Path.Combine(tempRoot, "new.tmp");
        File.WriteAllText(oldFile, "old");
        File.WriteAllText(newFile, "new");
        File.SetLastWriteTimeUtc(oldFile, new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc));
        File.SetLastWriteTimeUtc(newFile, new DateTime(2026, 7, 11, 10, 0, 0, DateTimeKind.Utc));

        var service = new SafeCleanupService(
            tempRoot,
            windowsTempDirectory: null,
            new DateTimeOffset(2026, 7, 11, 12, 0, 0, TimeSpan.Zero));
        var result = service.Scan(includeUserTemp: true, includeWindowsTemp: false, includeRecycleBin: false);

        AssertEqual(1, result.Items.Count);
        AssertEqual(oldFile, result.Items[0].Path);
        AssertEqual(CleanupTargetKind.UserTempFile, result.Items[0].Kind);
    }
    finally
    {
        Directory.Delete(tempRoot, recursive: true);
    }
}

static void TestSafeCleanupSkipsReparsePointDirectories()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), $"pc-guardian-cleanup-{Guid.NewGuid():N}");
    var reparseLikeDirectory = Path.Combine(tempRoot, "junction-like");
    Directory.CreateDirectory(reparseLikeDirectory);

    try
    {
        File.SetAttributes(reparseLikeDirectory, File.GetAttributes(reparseLikeDirectory) | FileAttributes.ReparsePoint);
        File.WriteAllText(Path.Combine(reparseLikeDirectory, "old.tmp"), "old");

        var service = new SafeCleanupService(
            tempRoot,
            windowsTempDirectory: null,
            new DateTimeOffset(2026, 7, 11, 12, 0, 0, TimeSpan.Zero));
        var result = service.Scan(includeUserTemp: true, includeWindowsTemp: false, includeRecycleBin: false);

        AssertEqual(0, result.Items.Count);
    }
    finally
    {
        File.SetAttributes(reparseLikeDirectory, FileAttributes.Directory);
        Directory.Delete(tempRoot, recursive: true);
    }
}

static void TestSafeCleanupDeletesOnlySelectedWhitelistedFiles()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), $"pc-guardian-cleanup-{Guid.NewGuid():N}");
    var outsideRoot = Path.Combine(Path.GetTempPath(), $"pc-guardian-outside-{Guid.NewGuid():N}");
    Directory.CreateDirectory(tempRoot);
    Directory.CreateDirectory(outsideRoot);

    try
    {
        var selectedFile = Path.Combine(tempRoot, "selected.tmp");
        var unselectedFile = Path.Combine(tempRoot, "unselected.tmp");
        var outsideFile = Path.Combine(outsideRoot, "outside.tmp");
        File.WriteAllText(selectedFile, "selected");
        File.WriteAllText(unselectedFile, "unselected");
        File.WriteAllText(outsideFile, "outside");

        var service = new SafeCleanupService(tempRoot, windowsTempDirectory: null, DateTimeOffset.Now);
        var result = service.Clean([
            new CleanupTarget("1", CleanupTargetKind.UserTempFile, "selected.tmp", selectedFile, 8, true),
            new CleanupTarget("2", CleanupTargetKind.UserTempFile, "unselected.tmp", unselectedFile, 10, false),
            new CleanupTarget("3", CleanupTargetKind.UserTempFile, "outside.tmp", outsideFile, 7, true)
        ]);

        AssertEqual(false, File.Exists(selectedFile));
        AssertEqual(true, File.Exists(unselectedFile));
        AssertEqual(true, File.Exists(outsideFile));
        AssertEqual(1, result.DeletedCount);
        AssertEqual(8L, result.FreedBytes);
        AssertEqual(1, result.SkippedCount);
    }
    finally
    {
        Directory.Delete(tempRoot, recursive: true);
        Directory.Delete(outsideRoot, recursive: true);
    }
}

static void TestSafeCleanupIgnoresInaccessibleEmptyDirectoryCleanupRoots()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), $"pc-guardian-cleanup-{Guid.NewGuid():N}");
    Directory.CreateDirectory(tempRoot);

    try
    {
        var inaccessibleRoot = @"C:\System Volume Information";
        if (!Directory.Exists(inaccessibleRoot))
        {
            inaccessibleRoot = @"C:\Windows\Temp";
        }

        var service = new SafeCleanupService(tempRoot, inaccessibleRoot, DateTimeOffset.Now);
        var result = service.Clean(Array.Empty<CleanupTarget>());

        AssertEqual(0, result.DeletedCount);
        AssertEqual(0L, result.FreedBytes);
    }
    finally
    {
        Directory.Delete(tempRoot, recursive: true);
    }
}

static void TestMainWindowUsesCyberHudSkinResources()
{
    var sourceRoot = FindSourceRoot();
    var xamlPath = Path.Combine(sourceRoot, "src", "PcGuardianLite.App", "MainWindow.xaml");
    var xaml = File.ReadAllText(xamlPath);

    AssertContains("CyberPanelBrush", xaml);
    AssertContains("CyberMetricCardStyle", xaml);
    AssertContains("CyberPulseStoryboard", xaml);
    AssertContains("#07111F", xaml);
    AssertContains("SYSTEM STATUS", xaml);
}

static void TestMainWindowUsesTabbedToolLayout()
{
    var sourceRoot = FindSourceRoot();
    var xamlPath = Path.Combine(sourceRoot, "src", "PcGuardianLite.App", "MainWindow.xaml");
    var xaml = File.ReadAllText(xamlPath);

    AssertContains("CyberTabControlStyle", xaml);
    AssertContains("Header=\"总览\"", xaml);
    AssertContains("Header=\"清理\"", xaml);
    AssertContains("Header=\"网络\"", xaml);
    AssertContains("Header=\"进程\"", xaml);
    AssertContains("Header=\"报告\"", xaml);
}

static string FindSourceRoot()
{
    var current = new DirectoryInfo(AppContext.BaseDirectory);

    while (current is not null)
    {
        if (File.Exists(Path.Combine(current.FullName, "PcGuardianLite.sln")))
        {
            return current.FullName;
        }

        current = current.Parent;
    }

    throw new DirectoryNotFoundException("Could not locate PcGuardianLite.sln from the test output directory.");
}

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new UnreachableException($"Expected '{expected}', got '{actual}'.");
    }
}

static void AssertContains(string expected, string actual)
{
    if (!actual.Contains(expected, StringComparison.Ordinal))
    {
        throw new UnreachableException($"Expected text to contain '{expected}'.");
    }
}

sealed class FakeMetricProvider : IMetricProvider
{
    public double CpuUsagePercent { get; set; }
    public double MemoryUsagePercent { get; set; }
    public double DiskUsagePercent { get; set; }
    public double? TemperatureCelsius { get; set; }
    public NetworkCounterSample NetworkSample { get; set; } = new(0, 0, DateTimeOffset.Now);

    public double GetCpuUsagePercent() => CpuUsagePercent;

    public double GetMemoryUsagePercent() => MemoryUsagePercent;

    public NetworkCounterSample GetNetworkSample() => NetworkSample;

    public double GetDiskUsagePercent() => DiskUsagePercent;

    public double? GetTemperatureCelsius() => TemperatureCelsius;
}
