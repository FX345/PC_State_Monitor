using System.Diagnostics;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using PcGuardianLite.Core;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using WinFormsContextMenuStrip = System.Windows.Forms.ContextMenuStrip;
using WinFormsNotifyIcon = System.Windows.Forms.NotifyIcon;
using WinFormsToolStripMenuItem = System.Windows.Forms.ToolStripMenuItem;
using WinFormsToolStripSeparator = System.Windows.Forms.ToolStripSeparator;

namespace PcGuardianLite.App;

public partial class MainWindow : Window
{
    private const double CollapsedWindowWidth = 196;
    private const double CollapsedWindowHeight = 214;
    private const double ExpandedWindowWidth = 1500;
    private const double ExpandedWindowHeight = 900;
    private readonly SystemMonitorService monitorService = new(new WindowsMetricProvider());
    private readonly SafeCleanupService cleanupService = new();
    private readonly SpeedTestService speedTestService = new();
    private readonly ObservableCollection<CleanupTargetViewModel> cleanupItems = new();
    private readonly DispatcherTimer refreshTimer = new();
    private readonly WinFormsNotifyIcon trayIcon;
    private readonly string scriptsRoot;
    private readonly string reportsRoot;
    private Point dragStartScreen;
    private double dragStartLeft;
    private double dragStartTop;
    private bool isDragging;
    private bool isMonitoring = true;
    private bool isTrayDisposed;
    private FloatingDisplayMode displayMode = FloatingDisplayMode.Memory;
    private int refreshTicks;

    public MainWindow()
    {
        InitializeComponent();

        CleanupListView.ItemsSource = cleanupItems;
        trayIcon = CreateTrayIcon();
        scriptsRoot = FindScriptsRoot();
        reportsRoot = FindReportsRoot(scriptsRoot);
        Loaded += MainWindow_Loaded;
        Closed += (_, _) => DisposeTrayIcon();

        refreshTimer.Interval = TimeSpan.FromSeconds(1);
        refreshTimer.Tick += (_, _) => RefreshSnapshot();
        refreshTimer.Start();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Width = CollapsedWindowWidth;
        Height = CollapsedWindowHeight;
        Left = SystemParameters.WorkArea.Right - Width - 24;
        Top = Math.Max(SystemParameters.WorkArea.Top + 24, SystemParameters.WorkArea.Bottom - Height - 24);
        RefreshSnapshot();
    }

    private void RefreshSnapshot()
    {
        if (!isMonitoring)
        {
            return;
        }

        var snapshot = monitorService.CaptureSnapshot();
        refreshTicks++;

        UpdateFloatingBall(snapshot);
        CpuText.Text = snapshot.CpuText;
        MemoryText.Text = snapshot.MemoryText;
        DownloadText.Text = snapshot.DownloadText;
        UploadText.Text = snapshot.UploadText;
        NetworkDownloadText.Text = snapshot.DownloadText;
        NetworkUploadText.Text = snapshot.UploadText;
        DiskText.Text = snapshot.DiskText;
        TemperatureText.Text = snapshot.TemperatureText;
        UpdateHealthAndWarnings(snapshot);

        if (refreshTicks % 5 == 1)
        {
            RefreshDiagnostics();
        }
    }

    private void FloatingBall_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        dragStartScreen = PointToScreen(e.GetPosition(this));
        dragStartLeft = Left;
        dragStartTop = Top;
        isDragging = false;
        FloatingBall.CaptureMouse();
    }

    private void FloatingBall_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || !FloatingBall.IsMouseCaptured)
        {
            return;
        }

        var current = PointToScreen(e.GetPosition(this));

        if (!isDragging &&
            Math.Abs(current.X - dragStartScreen.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(current.Y - dragStartScreen.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        isDragging = true;

        var position = DragPositionCalculator.Calculate(
            dragStartLeft,
            dragStartTop,
            dragStartScreen.X,
            dragStartScreen.Y,
            current.X,
            current.Y);

        Left = position.Left;
        Top = position.Top;
    }

    private void FloatingBall_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        FloatingBall.ReleaseMouseCapture();

        if (!isDragging)
        {
            TogglePanel();
        }
    }

    private void TogglePanel()
    {
        if (DetailPanel.Visibility == Visibility.Visible)
        {
            AnimatePanelClose();
            return;
        }

        ExpandWindowForPanel();
        DetailPanel.Visibility = Visibility.Visible;
        KeepPanelInsideWorkArea();
        AnimatePanelOpen();
    }

    private void ExpandWindowForPanel()
    {
        var ballRight = Left + Width;
        var ballTop = Top;

        Width = ExpandedWindowWidth;
        Height = ExpandedWindowHeight;
        Left = ballRight - Width;
        Top = ballTop;
    }

    private void CollapseWindowToBall()
    {
        var ballRight = Left + Width;
        var ballTop = Top;

        Width = CollapsedWindowWidth;
        Height = CollapsedWindowHeight;
        Left = ballRight - Width;
        Top = ballTop;
        KeepCollapsedWindowInsideWorkArea();
    }

    private void AnimatePanelOpen()
    {
        DetailPanel.Opacity = 0;
        PanelEntranceTransform.Y = -10;

        DetailPanel.BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });

        PanelEntranceTransform.BeginAnimation(
            TranslateTransform.YProperty,
            new DoubleAnimation(-10, 0, TimeSpan.FromMilliseconds(220))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });
    }

    private void AnimatePanelClose()
    {
        DetailPanel.BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(DetailPanel.Opacity, 0, TimeSpan.FromMilliseconds(140))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            });

        PanelEntranceTransform.BeginAnimation(
            TranslateTransform.YProperty,
            new DoubleAnimation(0, -8, TimeSpan.FromMilliseconds(140))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            });

        var closeTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(145)
        };
        closeTimer.Tick += (_, _) =>
        {
            closeTimer.Stop();
            DetailPanel.Visibility = Visibility.Collapsed;
            DetailPanel.Opacity = 1;
            PanelEntranceTransform.Y = 0;
            CollapseWindowToBall();
        };
        closeTimer.Start();
    }

    private void KeepPanelInsideWorkArea()
    {
        var workArea = SystemParameters.WorkArea;
        var desiredRight = Left + Width;
        var desiredBottom = Top + Height;

        if (desiredRight > workArea.Right)
        {
            Left = Math.Max(workArea.Left, workArea.Right - Width - 8);
        }

        if (desiredBottom > workArea.Bottom)
        {
            Top = Math.Max(workArea.Top + 8, workArea.Bottom - Height - 8);
        }

        if (Left < workArea.Left)
        {
            Left = workArea.Left + 8;
        }

        if (Top < workArea.Top)
        {
            Top = workArea.Top + 8;
        }
    }

    private void KeepCollapsedWindowInsideWorkArea()
    {
        var workArea = SystemParameters.WorkArea;

        if (Left + Width > workArea.Right)
        {
            Left = workArea.Right - Width - 8;
        }

        if (Top + Height > workArea.Bottom)
        {
            Top = workArea.Bottom - Height - 8;
        }

        if (Left < workArea.Left)
        {
            Left = workArea.Left + 8;
        }

        if (Top < workArea.Top)
        {
            Top = workArea.Top + 8;
        }
    }

    private void FloatingBallContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        PanelToggleMenuItem.Header = FloatingBallMenuTextFormatter.FormatPanelToggle(DetailPanel.Visibility == Visibility.Visible);
        MonitorToggleMenuItem.Header = FloatingBallMenuTextFormatter.FormatMonitorToggle(isMonitoring);
    }

    private void PanelToggleMenuItem_Click(object sender, RoutedEventArgs e)
    {
        TogglePanel();
    }

    private void MonitorToggleMenuItem_Click(object sender, RoutedEventArgs e)
    {
        isMonitoring = !isMonitoring;

        if (isMonitoring)
        {
            refreshTimer.Start();
            StatusText.Text = "监控已继续";
            RefreshSnapshot();
        }
        else
        {
            refreshTimer.Stop();
            StatusText.Text = "监控已暂停";
        }
    }

    private void DisplayMemoryMenuItem_Click(object sender, RoutedEventArgs e)
    {
        displayMode = FloatingDisplayMode.Memory;
        RefreshSnapshot();
    }

    private void DisplayCpuMenuItem_Click(object sender, RoutedEventArgs e)
    {
        displayMode = FloatingDisplayMode.Cpu;
        RefreshSnapshot();
    }

    private void DisplayNetworkMenuItem_Click(object sender, RoutedEventArgs e)
    {
        displayMode = FloatingDisplayMode.Network;
        RefreshSnapshot();
    }

    private void DisplayAutoMenuItem_Click(object sender, RoutedEventArgs e)
    {
        displayMode = FloatingDisplayMode.Auto;
        RefreshSnapshot();
    }

    private void Opacity95MenuItem_Click(object sender, RoutedEventArgs e)
    {
        Opacity = 0.95;
    }

    private void Opacity75MenuItem_Click(object sender, RoutedEventArgs e)
    {
        Opacity = 0.75;
    }

    private void Opacity55MenuItem_Click(object sender, RoutedEventArgs e)
    {
        Opacity = 0.55;
    }

    private void HideToTrayMenuItem_Click(object sender, RoutedEventArgs e)
    {
        HideToTray();
    }

    private void ClosePanel_Click(object sender, RoutedEventArgs e)
    {
        if (DetailPanel.Visibility == Visibility.Visible)
        {
            AnimatePanelClose();
        }
    }

    private void OpenCleanupTab_Click(object sender, RoutedEventArgs e)
    {
        MainTabs.SelectedIndex = 1;
    }

    private void OpenNetworkTab_Click(object sender, RoutedEventArgs e)
    {
        MainTabs.SelectedIndex = 2;
    }

    private void OpenProcessTab_Click(object sender, RoutedEventArgs e)
    {
        MainTabs.SelectedIndex = 3;
    }

    private void OpenReportsTab_Click(object sender, RoutedEventArgs e)
    {
        MainTabs.SelectedIndex = 4;
    }

    private void RunPcReport_Click(object sender, RoutedEventArgs e)
    {
        RunScript("pc_report.ps1");
    }

    private void RunNetworkReport_Click(object sender, RoutedEventArgs e)
    {
        RunScript("network_report.ps1");
    }

    private async void RunSpeedTest_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SetSpeedTestActivity(true);
            SpeedTestStatusText.Text = "正在测速：会进行一次小样本下载和上传，用于估算当前网络带宽。";
            StatusText.Text = "正在进行网络测速...";

            var result = await speedTestService.RunAsync();

            SpeedTestDownloadText.Text = SpeedTestFormatter.FormatMegabitsPerSecond(result.DownloadBytesPerSecond);
            SpeedTestUploadText.Text = SpeedTestFormatter.FormatMegabitsPerSecond(result.UploadBytesPerSecond);
            SpeedTestStatusText.Text =
                $"测速完成：下载样本 {FormatSize(result.DownloadedBytes)}，上传样本 {FormatSize(result.UploadedBytes)}。";
            StatusText.Text = "网络测速完成";
        }
        catch (Exception ex)
        {
            SpeedTestStatusText.Text = $"测速失败：{ex.Message}";
            StatusText.Text = "网络测速失败";
        }
        finally
        {
            SetSpeedTestActivity(false);
        }
    }

    private void RunFolderRadar_Click(object sender, RoutedEventArgs e)
    {
        RunScript("folder_radar.ps1", @"-Path D:\ -Top 20");
    }

    private void OpenReports_Click(object sender, RoutedEventArgs e)
    {
        var reportDir = Path.Combine(reportsRoot, "ScriptReports");
        Directory.CreateDirectory(reportDir);

        Process.Start(new ProcessStartInfo
        {
            FileName = reportDir,
            UseShellExecute = true
        });
    }

    private async void ScanCleanup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SetCleanupActivity(true);
            await RefreshCleanupScanAsync();
        }
        catch (Exception ex)
        {
            CleanupSummaryText.Text = $"扫描失败：{ex.Message}";
            StatusText.Text = CleanupSummaryText.Text;
        }
        finally
        {
            SetCleanupActivity(false);
        }
    }

    private async Task RefreshCleanupScanAsync()
    {
        CleanupSummaryText.Text = "正在扫描安全清理项...";
        cleanupItems.Clear();

        var includeUserTemp = CleanupUserTempCheckBox.IsChecked == true;
        var includeWindowsTemp = CleanupWindowsTempCheckBox.IsChecked == true;
        var includeRecycleBin = CleanupRecycleBinCheckBox.IsChecked == true;

        var result = await Task.Run(() => cleanupService.Scan(includeUserTemp, includeWindowsTemp, includeRecycleBin));

        foreach (var item in result.Items)
        {
            cleanupItems.Add(new CleanupTargetViewModel(item));
        }

        CleanupSummaryText.Text = result.Items.Count == 0
            ? $"没有发现可安全清理的项目，跳过 {result.SkippedCount} 项。"
            : $"发现 {result.Items.Count} 项，可清理 {FormatSize(result.TotalBytes)}，跳过 {result.SkippedCount} 项。";
    }

    private async void RunCleanup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var selectedTargets = cleanupItems
                .Where(item => item.IsSelected)
                .Select(item => item.ToTarget())
                .ToArray();

            if (selectedTargets.Length == 0)
            {
                CleanupSummaryText.Text = "请先勾选要清理的项目。";
                return;
            }

            if (selectedTargets.Any(target => target.Kind == CleanupTargetKind.RecycleBin))
            {
                var confirmRecycleBin = System.Windows.MessageBox.Show(
                    "你勾选了回收站。清空回收站后通常无法直接恢复，确认继续吗？",
                    "确认清空回收站",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirmRecycleBin != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            var confirm = System.Windows.MessageBox.Show(
                $"将清理 {selectedTargets.Length} 个选中项目，预计释放 {FormatSize(selectedTargets.Sum(target => target.SizeBytes))}。确认继续吗？",
                "确认清理",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            CleanupSummaryText.Text = "正在清理选中项...";
            SetCleanupActivity(true);
            var result = await Task.Run(() => cleanupService.Clean(selectedTargets));

            var completionText = $"清理完成：删除 {result.DeletedCount} 项，释放 {FormatSize(result.FreedBytes)}，跳过 {result.SkippedCount} 项。";
            await RefreshCleanupScanAsync();
            CleanupSummaryText.Text = $"{completionText} 列表已刷新。";
            StatusText.Text = completionText;
        }
        catch (Exception ex)
        {
            CleanupSummaryText.Text = $"清理失败：{ex.Message}";
            StatusText.Text = CleanupSummaryText.Text;
        }
        finally
        {
            SetCleanupActivity(false);
        }
    }

    private void SetCleanupActivity(bool isActive)
    {
        CleanupActivityBar.Visibility = isActive ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SetSpeedTestActivity(bool isActive)
    {
        RunSpeedTestButton.IsEnabled = !isActive;
        SpeedTestActivityBar.Visibility = isActive ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        ExitApplication();
    }

    private void RunScript(string scriptName, string? arguments = null)
    {
        var scriptPath = Path.Combine(scriptsRoot, scriptName);
        var result = ScriptLauncher.RunScript(scriptPath, arguments);
        StatusText.Text = result.Message;
    }

    private void UpdateFloatingBall(SystemSnapshot snapshot)
    {
        var activeMode = ResolveDisplayMode();

        switch (activeMode)
        {
            case FloatingDisplayMode.Cpu:
                BallMemoryText.Text = $"CPU {snapshot.CpuText}";
                BallDownloadText.Text = $"内存 {snapshot.MemoryText}";
                SetBallStatus(ParsePercent(snapshot.CpuText), 70, 90);
                break;
            case FloatingDisplayMode.Network:
                BallMemoryText.Text = "网速";
                BallDownloadText.Text = $"下 {snapshot.DownloadText}";
                SetBallStatus(Math.Max(ParseSpeed(snapshot.DownloadText), ParseSpeed(snapshot.UploadText)), 5 * 1024 * 1024, 15 * 1024 * 1024);
                break;
            default:
                BallMemoryText.Text = FloatingBallTextFormatter.FormatMemory(snapshot.MemoryText);
                BallDownloadText.Text = FloatingBallTextFormatter.FormatDownload(snapshot.DownloadText);
                SetBallStatus(ParsePercent(snapshot.MemoryText), 70, 90);
                break;
        }
    }

    private FloatingDisplayMode ResolveDisplayMode()
    {
        if (displayMode != FloatingDisplayMode.Auto)
        {
            return displayMode;
        }

        return ((refreshTicks / 5) % 3) switch
        {
            0 => FloatingDisplayMode.Memory,
            1 => FloatingDisplayMode.Cpu,
            _ => FloatingDisplayMode.Network
        };
    }

    private void SetBallStatus(double value, double warningThreshold, double criticalThreshold)
    {
        var status = MetricStatusCalculator.FromPercent(value, warningThreshold, criticalThreshold);
        var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(StatusColorPalette.GetHex(status));
        FloatingBall.BorderBrush = new SolidColorBrush(color);
    }

    private void UpdateHealthAndWarnings(SystemSnapshot snapshot)
    {
        var cpu = ParsePercent(snapshot.CpuText);
        var memory = ParsePercent(snapshot.MemoryText);
        var disk = ParsePercent(snapshot.DiskText);
        var downloadBytes = ParseSpeed(snapshot.DownloadText);
        var uploadBytes = ParseSpeed(snapshot.UploadText);
        var healthScore = HealthScoreCalculator.Calculate(cpu, memory, disk, downloadBytes, uploadBytes);

        HealthScoreText.Text = healthScore.ToString();
        AnimateHealthScoreBrush(healthScore);
        DiskWarningText.Text = disk >= 90
            ? "磁盘剩余空间低于 10%，建议尽快整理"
            : "磁盘空间正常";
        DiskWarningText.Foreground = disk >= 90
            ? System.Windows.Media.Brushes.Firebrick
            : System.Windows.Media.Brushes.SlateGray;
        HealthReasonText.Text = BuildHealthReason(cpu, memory, disk, downloadBytes + uploadBytes);
    }

    private void AnimateHealthScoreBrush(int healthScore)
    {
        var targetColor = healthScore >= 80
            ? System.Windows.Media.Color.FromRgb(0x2D, 0xE2, 0xFF)
            : healthScore >= 60
                ? System.Windows.Media.Color.FromRgb(0xFF, 0x9F, 0x1C)
                : System.Windows.Media.Color.FromRgb(0xEF, 0x44, 0x44);

        if (HealthScoreText.Foreground is not SolidColorBrush brush || brush.IsFrozen)
        {
            brush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF6, 0xF7, 0xF2));
            HealthScoreText.Foreground = brush;
        }

        brush.BeginAnimation(
            SolidColorBrush.ColorProperty,
            new ColorAnimation(targetColor, TimeSpan.FromMilliseconds(520))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });
    }

    private void RefreshDiagnostics()
    {
        var processLines = ProcessRankingProvider
            .GetTopProcesses(5)
            .Select(process => $"{process.Name}  {process.MemoryMb:0.#} MB  CPU {process.CpuSeconds:0.#}s");

        var rankingText = string.Join(Environment.NewLine, processLines);
        ProcessRankingText.Text = rankingText;
        OverviewProcessRankingText.Text = rankingText;
    }

    private static double ParsePercent(string text)
    {
        return double.TryParse(text.Replace("%", string.Empty).Trim(), out var value)
            ? value
            : 0;
    }

    private static double ParseSpeed(string text)
    {
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2 || !double.TryParse(parts[0], out var value))
        {
            return 0;
        }

        return parts[1] switch
        {
            "KB/s" => value * 1024,
            "MB/s" => value * 1024 * 1024,
            _ => value
        };
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        var kb = bytes / 1024d;
        if (kb < 1024)
        {
            return $"{kb:0.#} KB";
        }

        var mb = kb / 1024d;
        if (mb < 1024)
        {
            return $"{mb:0.#} MB";
        }

        return $"{mb / 1024d:0.#} GB";
    }

    private static string BuildHealthReason(double cpu, double memory, double disk, double networkBytesPerSecond)
    {
        var reasons = new List<string>();

        if (cpu >= 90)
        {
            reasons.Add("CPU 压力高");
        }
        else if (cpu >= 50)
        {
            reasons.Add("CPU 偏忙");
        }

        if (memory >= 90)
        {
            reasons.Add("内存压力高");
        }
        else if (memory >= 60)
        {
            reasons.Add("内存偏高");
        }

        if (disk >= 95)
        {
            reasons.Add("磁盘空间紧张");
        }
        else if (disk >= 75)
        {
            reasons.Add("磁盘占用偏高");
        }

        if (networkBytesPerSecond >= 20 * 1024 * 1024)
        {
            reasons.Add("网络流量较高");
        }

        return reasons.Count == 0
            ? "状态良好：CPU、内存、磁盘压力都较低"
            : "评分原因：" + string.Join("，", reasons);
    }

    private WinFormsNotifyIcon CreateTrayIcon()
    {
        var showMenuItem = new WinFormsToolStripMenuItem(TrayMenuTextFormatter.ShowFloatingBallText);
        showMenuItem.Click += (_, _) => ShowFromTray();

        var exitMenuItem = new WinFormsToolStripMenuItem("退出程序");
        exitMenuItem.Click += (_, _) => ExitApplication();

        var menu = new WinFormsContextMenuStrip();
        menu.Items.Add(showMenuItem);
        menu.Items.Add(new WinFormsToolStripSeparator());
        menu.Items.Add(exitMenuItem);

        var notifyIcon = new WinFormsNotifyIcon
        {
            ContextMenuStrip = menu,
            Icon = System.Drawing.SystemIcons.Application,
            Text = "PcGuardianLite",
            Visible = true
        };

        notifyIcon.DoubleClick += (_, _) => ShowFromTray();

        return notifyIcon;
    }

    private void HideToTray()
    {
        DetailPanel.Visibility = Visibility.Collapsed;
        DetailPanel.Opacity = 1;
        CollapseWindowToBall();
        Hide();
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ExitApplication()
    {
        DisposeTrayIcon();
        Close();
    }

    private void DisposeTrayIcon()
    {
        if (isTrayDisposed)
        {
            return;
        }

        trayIcon.Visible = false;
        trayIcon.Dispose();
        isTrayDisposed = true;
    }

    private static string FindScriptsRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "pc_report.ps1")) &&
                File.Exists(Path.Combine(current.FullName, "network_report.ps1")) &&
                File.Exists(Path.Combine(current.FullName, "folder_radar.ps1")))
            {
                return current.FullName;
            }

            var toolsScriptsDirectory = Path.Combine(current.FullName, "tools", "scripts");
            if (File.Exists(Path.Combine(toolsScriptsDirectory, "pc_report.ps1")) &&
                File.Exists(Path.Combine(toolsScriptsDirectory, "network_report.ps1")) &&
                File.Exists(Path.Combine(toolsScriptsDirectory, "folder_radar.ps1")))
            {
                return toolsScriptsDirectory;
            }

            var scriptsDirectory = Path.Combine(current.FullName, "scripts");
            if (File.Exists(Path.Combine(scriptsDirectory, "pc_report.ps1")) &&
                File.Exists(Path.Combine(scriptsDirectory, "network_report.ps1")) &&
                File.Exists(Path.Combine(scriptsDirectory, "folder_radar.ps1")))
            {
                return scriptsDirectory;
            }

            current = current.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    private static string FindReportsRoot(string scriptsDirectory)
    {
        var scriptInfo = new DirectoryInfo(scriptsDirectory);
        var toolsInfo = scriptInfo.Parent;
        var installInfo = toolsInfo?.Parent;

        if (string.Equals(scriptInfo.Name, "scripts", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(toolsInfo?.Name, "tools", StringComparison.OrdinalIgnoreCase) &&
            installInfo is not null)
        {
            return installInfo.FullName;
        }

        return scriptsDirectory;
    }

    private sealed class CleanupTargetViewModel
    {
        private readonly CleanupTarget target;

        public CleanupTargetViewModel(CleanupTarget target)
        {
            this.target = target;
            IsSelected = target.IsSelected;
        }

        public bool IsSelected { get; set; }

        public string DisplayName => target.DisplayName;

        public string DetailText => $"{FormatSize(target.SizeBytes)}  {target.Path}";

        public CleanupTarget ToTarget()
        {
            return target with { IsSelected = IsSelected };
        }
    }
}
