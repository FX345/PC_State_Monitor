using System.Diagnostics;

namespace PcGuardianLite.Core;

public static class ScriptLauncher
{
    public static ProcessStartInfo BuildPowerShellStartInfo(string scriptPath, string? arguments = null)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(scriptPath);

        foreach (var argument in SplitArguments(arguments))
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    public static ScriptLaunchResult RunScript(string scriptPath, string? arguments = null)
    {
        if (!File.Exists(scriptPath))
        {
            return new ScriptLaunchResult(false, "Script not found.");
        }

        try
        {
            Process.Start(BuildPowerShellStartInfo(scriptPath, arguments));
            return new ScriptLaunchResult(true, "Script started.");
        }
        catch (Exception ex)
        {
            return new ScriptLaunchResult(false, ex.Message);
        }
    }

    private static IEnumerable<string> SplitArguments(string? arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            yield break;
        }

        foreach (var argument in arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            yield return argument;
        }
    }
}
