namespace PcGuardianLite.Core;

public static class UninstallPlanner
{
    public const string AppName = "PcGuardianLite";
    public const string AppExecutableName = "PcGuardianLite.exe";
    public const string UninstallArgument = "--uninstall";
    public const string UninstallShortcutName = "Uninstall PcGuardianLite.bat";

    public static string GetUninstallCommand(string installDirectory, string executableName)
    {
        if (string.IsNullOrWhiteSpace(installDirectory))
        {
            throw new ArgumentException("Install directory is required.", nameof(installDirectory));
        }

        if (string.IsNullOrWhiteSpace(executableName))
        {
            throw new ArgumentException("Executable name is required.", nameof(executableName));
        }

        return $"\"{Path.Combine(installDirectory, executableName)}\" {UninstallArgument}";
    }

    public static string GetCurrentUserUninstallRegistryPath(string appName)
    {
        if (string.IsNullOrWhiteSpace(appName))
        {
            throw new ArgumentException("App name is required.", nameof(appName));
        }

        return $@"Software\Microsoft\Windows\CurrentVersion\Uninstall\{appName}";
    }

    public static string GetUninstallShortcutPath(string installDirectory)
    {
        if (string.IsNullOrWhiteSpace(installDirectory))
        {
            throw new ArgumentException("Install directory is required.", nameof(installDirectory));
        }

        return Path.Combine(installDirectory, UninstallShortcutName);
    }
}
