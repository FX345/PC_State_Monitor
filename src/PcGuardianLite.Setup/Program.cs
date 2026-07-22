using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using PcGuardianLite.Core;

namespace PcGuardianLite.Setup;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new InstallerForm());
    }
}

internal sealed class InstallerForm : Form
{
    private const string AppName = "PcGuardianLite";
    private readonly TextBox _installPathTextBox;
    private readonly CheckBox _desktopShortcutCheckBox;
    private readonly CheckBox _launchAfterInstallCheckBox;
    private readonly Button _installButton;
    private readonly Label _statusLabel;
    private readonly ProgressBar _progressBar;

    public InstallerForm()
    {
        Text = "PcGuardianLite 安装器";
        Width = 540;
        Height = 340;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var installPath = InstallerPathPlanner.GetInstallDirectory(localAppData, AppName);

        var titleLabel = new Label
        {
            Text = "安装 PcGuardianLite",
            Left = 26,
            Top = 22,
            Width = 470,
            Height = 30,
            Font = new Font(Font.FontFamily, 14, FontStyle.Bold)
        };

        var descriptionLabel = new Label
        {
            Text = "轻量电脑状态悬浮球，包含主程序、报告脚本和安全清理工具。",
            Left = 26,
            Top = 58,
            Width = 470,
            Height = 24
        };

        var pathLabel = new Label
        {
            Text = "安装位置",
            Left = 26,
            Top = 96,
            Width = 470,
            Height = 20
        };

        _installPathTextBox = new TextBox
        {
            Left = 26,
            Top = 120,
            Width = 470,
            Height = 26,
            ReadOnly = true,
            Text = installPath
        };

        _desktopShortcutCheckBox = new CheckBox
        {
            Left = 26,
            Top = 160,
            Width = 470,
            Height = 24,
            Text = "在桌面创建快捷方式",
            Checked = true
        };

        _launchAfterInstallCheckBox = new CheckBox
        {
            Left = 26,
            Top = 188,
            Width = 470,
            Height = 24,
            Text = "安装完成后立即启动",
            Checked = true
        };

        _statusLabel = new Label
        {
            Left = 26,
            Top = 224,
            Width = 470,
            Height = 20,
            Text = "准备安装"
        };

        _progressBar = new ProgressBar
        {
            Left = 26,
            Top = 250,
            Width = 330,
            Height = 18,
            Style = ProgressBarStyle.Blocks
        };

        _installButton = new Button
        {
            Left = 374,
            Top = 240,
            Width = 122,
            Height = 36,
            Text = "安装"
        };
        _installButton.Click += InstallButton_Click;

        Controls.Add(titleLabel);
        Controls.Add(descriptionLabel);
        Controls.Add(pathLabel);
        Controls.Add(_installPathTextBox);
        Controls.Add(_desktopShortcutCheckBox);
        Controls.Add(_launchAfterInstallCheckBox);
        Controls.Add(_statusLabel);
        Controls.Add(_progressBar);
        Controls.Add(_installButton);
    }

    private void InstallButton_Click(object? sender, EventArgs e)
    {
        try
        {
            SetInstallingState();
            var installDirectory = _installPathTextBox.Text;
            InstallPayload(installDirectory);
            CreateUninstallShortcut(installDirectory);
            RegisterUninstallEntry(installDirectory);

            if (_desktopShortcutCheckBox.Checked)
            {
                CreateDesktopShortcut(installDirectory);
            }

            if (_launchAfterInstallCheckBox.Checked)
            {
                LaunchApp(installDirectory);
            }

            _progressBar.Style = ProgressBarStyle.Blocks;
            _progressBar.Value = 100;
            _statusLabel.Text = "安装完成";
            MessageBox.Show("PcGuardianLite 已安装完成。", "安装完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
        catch (Exception ex)
        {
            _progressBar.Style = ProgressBarStyle.Blocks;
            _progressBar.Value = 0;
            _statusLabel.Text = "安装失败";
            _installButton.Enabled = true;
            MessageBox.Show(
                $"安装失败：{ex.Message}\n\n如果程序正在运行，请先从托盘退出后再安装。",
                "安装失败",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void SetInstallingState()
    {
        _installButton.Enabled = false;
        _statusLabel.Text = "正在安装...";
        _progressBar.Style = ProgressBarStyle.Marquee;
    }

    private static void InstallPayload(string installDirectory)
    {
        Directory.CreateDirectory(installDirectory);

        using var payloadStream = OpenPayloadStream();
        ZipFile.ExtractToDirectory(payloadStream, installDirectory, overwriteFiles: true);

        if (!InstallerPayloadManifest.HasRequiredFiles(installDirectory))
        {
            throw new InvalidOperationException("安装包缺少必要文件。");
        }
    }

    private static Stream OpenPayloadStream()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith("payload.zip", StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            throw new InvalidOperationException("安装包没有内嵌程序文件。");
        }

        return assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("无法读取内嵌程序文件。");
    }

    private static void CreateDesktopShortcut(string installDirectory)
    {
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var shortcutPath = InstallerPathPlanner.GetDesktopShortcutPath(desktopPath, AppName);
        var executablePath = Path.Combine(installDirectory, "PcGuardianLite.exe");

        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("无法创建快捷方式。");
        object? shell = null;
        object? shortcut = null;

        try
        {
            shell = Activator.CreateInstance(shellType);
            shortcut = shellType.InvokeMember(
                "CreateShortcut",
                BindingFlags.InvokeMethod,
                null,
                shell,
                [shortcutPath]);

            if (shortcut is null)
            {
                throw new InvalidOperationException("无法创建快捷方式。");
            }

            SetComProperty(shortcut, "TargetPath", executablePath);
            SetComProperty(shortcut, "WorkingDirectory", installDirectory);
            SetComProperty(shortcut, "Description", "PcGuardianLite 轻量电脑状态悬浮球");
            shortcut.GetType().InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
        }
        finally
        {
            ReleaseComObject(shortcut);
            ReleaseComObject(shell);
        }
    }

    private static void CreateUninstallShortcut(string installDirectory)
    {
        var shortcutPath = UninstallPlanner.GetUninstallShortcutPath(installDirectory);
        var uninstallCommand = UninstallPlanner.GetUninstallCommand(installDirectory, UninstallPlanner.AppExecutableName);

        File.WriteAllLines(shortcutPath,
        [
            "@echo off",
            uninstallCommand
        ]);
    }

    private static void RegisterUninstallEntry(string installDirectory)
    {
        var registryPath = UninstallPlanner.GetCurrentUserUninstallRegistryPath(UninstallPlanner.AppName);
        using var key = Registry.CurrentUser.CreateSubKey(registryPath)
            ?? throw new InvalidOperationException("无法创建卸载注册表项。");

        var executablePath = Path.Combine(installDirectory, UninstallPlanner.AppExecutableName);
        var uninstallCommand = UninstallPlanner.GetUninstallCommand(installDirectory, UninstallPlanner.AppExecutableName);

        key.SetValue("DisplayName", UninstallPlanner.AppName, RegistryValueKind.String);
        key.SetValue("DisplayVersion", "1.0.0", RegistryValueKind.String);
        key.SetValue("Publisher", "PcGuardianLite", RegistryValueKind.String);
        key.SetValue("InstallLocation", installDirectory, RegistryValueKind.String);
        key.SetValue("DisplayIcon", executablePath, RegistryValueKind.String);
        key.SetValue("UninstallString", uninstallCommand, RegistryValueKind.String);
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        key.SetValue("EstimatedSize", CalculateInstalledSizeKb(installDirectory), RegistryValueKind.DWord);
    }

    private static int CalculateInstalledSizeKb(string installDirectory)
    {
        if (!Directory.Exists(installDirectory))
        {
            return 0;
        }

        var totalBytes = Directory.EnumerateFiles(installDirectory, "*", SearchOption.AllDirectories)
            .Sum(filePath => new FileInfo(filePath).Length);

        return (int)Math.Max(1, totalBytes / 1024);
    }

    private static void SetComProperty(object target, string propertyName, object value)
    {
        target.GetType().InvokeMember(propertyName, BindingFlags.SetProperty, null, target, [value]);
    }

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            Marshal.FinalReleaseComObject(value);
        }
    }

    private static void LaunchApp(string installDirectory)
    {
        var executablePath = Path.Combine(installDirectory, "PcGuardianLite.exe");
        Process.Start(new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = installDirectory,
            UseShellExecute = true
        });
    }
}
