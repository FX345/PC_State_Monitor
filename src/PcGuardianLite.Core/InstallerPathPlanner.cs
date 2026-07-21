namespace PcGuardianLite.Core;

public static class InstallerPathPlanner
{
    public static string GetInstallDirectory(string localAppDataPath, string appFolderName)
    {
        if (string.IsNullOrWhiteSpace(localAppDataPath))
        {
            throw new ArgumentException("Local app data path is required.", nameof(localAppDataPath));
        }

        if (string.IsNullOrWhiteSpace(appFolderName))
        {
            throw new ArgumentException("App folder name is required.", nameof(appFolderName));
        }

        return Path.Combine(localAppDataPath, appFolderName);
    }

    public static string GetDesktopShortcutPath(string desktopPath, string appName)
    {
        if (string.IsNullOrWhiteSpace(desktopPath))
        {
            throw new ArgumentException("Desktop path is required.", nameof(desktopPath));
        }

        if (string.IsNullOrWhiteSpace(appName))
        {
            throw new ArgumentException("App name is required.", nameof(appName));
        }

        return Path.Combine(desktopPath, $"{appName}.lnk");
    }
}
