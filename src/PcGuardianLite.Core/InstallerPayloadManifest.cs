namespace PcGuardianLite.Core;

public static class InstallerPayloadManifest
{
    public static IReadOnlyList<string> RequiredFileNames { get; } =
    [
        "PcGuardianLite.exe",
        "pc_report.ps1",
        "network_report.ps1",
        "folder_radar.ps1",
        "ai_review_pack.ps1",
        "cmd_for_folder_radar.txt"
    ];

    public static bool HasRequiredFiles(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return false;
        }

        return RequiredFileNames.All(fileName => File.Exists(Path.Combine(directory, fileName)));
    }
}
