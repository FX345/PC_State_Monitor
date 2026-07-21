using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using PcGuardianLite.Core;
using Forms = System.Windows.Forms;

namespace PcGuardianLite.App;

internal static class UninstallRunner
{
    private static readonly string[] InstalledFileNames =
    [
        .. InstallerPayloadManifest.RequiredFileNames,
        UninstallPlanner.UninstallShortcutName
    ];

    public static bool IsUninstallRequest(string[] args)
    {
        return args.Any(arg =>
            string.Equals(arg, UninstallPlanner.UninstallArgument, StringComparison.OrdinalIgnoreCase)
            || string.Equals(arg, "/uninstall", StringComparison.OrdinalIgnoreCase));
    }

    public static void RunInteractive()
    {
        var installDirectory = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
        var confirm = Forms.MessageBox.Show(
            "Uninstall PcGuardianLite from this computer?\n\nGenerated reports in ScriptReports will be kept.",
            "Uninstall PcGuardianLite",
            Forms.MessageBoxButtons.YesNo,
            Forms.MessageBoxIcon.Question);

        if (confirm != Forms.DialogResult.Yes)
        {
            return;
        }

        CloseRunningAppInstances(installDirectory);
        DeleteDesktopShortcut();
        DeleteUninstallRegistryEntry();

        Forms.MessageBox.Show(
            "PcGuardianLite has been uninstalled.\n\nReport files were kept if they were present.",
            "Uninstall complete",
            Forms.MessageBoxButtons.OK,
            Forms.MessageBoxIcon.Information);

        StartDeferredCleanup(installDirectory);
    }

    private static void CloseRunningAppInstances(string installDirectory)
    {
        var currentProcessId = Environment.ProcessId;
        var installedExecutablePath = Path.Combine(installDirectory, UninstallPlanner.AppExecutableName);

        foreach (var process in Process.GetProcessesByName("PcGuardianLite"))
        {
            using (process)
            {
                if (process.Id == currentProcessId || !IsInstalledAppProcess(process, installedExecutablePath))
                {
                    continue;
                }

                try
                {
                    process.CloseMainWindow();
                    if (!process.WaitForExit(milliseconds: 2000) && !process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                        process.WaitForExit(milliseconds: 3000);
                    }
                }
                catch
                {
                    // If another process exits while uninstall is running, cleanup can continue.
                }
            }
        }
    }

    private static bool IsInstalledAppProcess(Process process, string installedExecutablePath)
    {
        try
        {
            return string.Equals(process.MainModule?.FileName, installedExecutablePath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static void DeleteDesktopShortcut()
    {
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var shortcutPath = InstallerPathPlanner.GetDesktopShortcutPath(desktopPath, UninstallPlanner.AppName);

        if (File.Exists(shortcutPath))
        {
            File.Delete(shortcutPath);
        }
    }

    private static void DeleteUninstallRegistryEntry()
    {
        var registryPath = UninstallPlanner.GetCurrentUserUninstallRegistryPath(UninstallPlanner.AppName);
        Registry.CurrentUser.DeleteSubKeyTree(registryPath, throwOnMissingSubKey: false);
    }

    private static void StartDeferredCleanup(string installDirectory)
    {
        var cleanupScript = Path.Combine(Path.GetTempPath(), $"PcGuardianLite-uninstall-{Guid.NewGuid():N}.cmd");
        var executablePath = Path.Combine(installDirectory, UninstallPlanner.AppExecutableName);
        var lines = new List<string>
        {
            "@echo off",
            "timeout /t 1 /nobreak >nul",
            "for /l %%i in (1,1,30) do (",
            $"  del /f /q {QuoteForCmd(executablePath)} >nul 2>nul",
            $"  if not exist {QuoteForCmd(executablePath)} goto delete_rest",
            "  timeout /t 1 /nobreak >nul",
            ")",
            ":delete_rest"
        };

        foreach (var fileName in InstalledFileNames)
        {
            if (string.Equals(fileName, UninstallPlanner.AppExecutableName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            lines.Add($"del /f /q {QuoteForCmd(Path.Combine(installDirectory, fileName))} >nul 2>nul");
        }

        lines.Add($"rmdir {QuoteForCmd(installDirectory)} >nul 2>nul");
        lines.Add("del /f /q \"%~f0\" >nul 2>nul");
        File.WriteAllLines(cleanupScript, lines);

        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"\"{cleanupScript}\"\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden
        });
    }

    private static string QuoteForCmd(string value)
    {
        return $"\"{value}\"";
    }
}
