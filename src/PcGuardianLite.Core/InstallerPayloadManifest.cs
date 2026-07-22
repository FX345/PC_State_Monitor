namespace PcGuardianLite.Core;

public static class InstallerPayloadManifest
{
    public static string ScriptsRelativeDirectory { get; } = Path.Combine("tools", "scripts");

    public static IReadOnlyList<string> RequiredScriptFileNames { get; } =
    [
        "pc_report.ps1",
        "network_report.ps1",
        "folder_radar.ps1",
        "ai_review_pack.ps1",
        "cmd_for_folder_radar.txt"
    ];

    public static IReadOnlyList<string> RequiredRelativePaths { get; } =
    [
        "PcGuardianLite.exe",
        .. RequiredScriptFileNames.Select(fileName => Path.Combine(ScriptsRelativeDirectory, fileName))
    ];

    public static bool HasRequiredFiles(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return false;
        }

        return RequiredRelativePaths.All(relativePath => File.Exists(Path.Combine(directory, relativePath)));
    }
}
