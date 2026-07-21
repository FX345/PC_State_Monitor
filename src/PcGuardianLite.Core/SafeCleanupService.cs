using System.Runtime.InteropServices;

namespace PcGuardianLite.Core;

public sealed class SafeCleanupService
{
    private static readonly TimeSpan UserTempMinimumAge = TimeSpan.FromHours(24);
    private static readonly TimeSpan WindowsTempMinimumAge = TimeSpan.FromDays(7);

    private readonly string userTempDirectory;
    private readonly string? windowsTempDirectory;
    private readonly DateTimeOffset now;

    public SafeCleanupService()
        : this(
            Path.GetTempPath(),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"),
            DateTimeOffset.Now)
    {
    }

    public SafeCleanupService(string userTempDirectory, string? windowsTempDirectory, DateTimeOffset now)
    {
        this.userTempDirectory = NormalizeDirectory(userTempDirectory);
        this.windowsTempDirectory = string.IsNullOrWhiteSpace(windowsTempDirectory)
            ? null
            : NormalizeDirectory(windowsTempDirectory);
        this.now = now;
    }

    public CleanupScanResult Scan(bool includeUserTemp, bool includeWindowsTemp, bool includeRecycleBin)
    {
        var items = new List<CleanupTarget>();
        var skippedCount = 0;

        if (includeUserTemp)
        {
            skippedCount += ScanDirectory(
                userTempDirectory,
                CleanupTargetKind.UserTempFile,
                "User temp",
                now - UserTempMinimumAge,
                items);
        }

        if (includeWindowsTemp && windowsTempDirectory is not null)
        {
            skippedCount += ScanDirectory(
                windowsTempDirectory,
                CleanupTargetKind.WindowsTempFile,
                "Windows temp",
                now - WindowsTempMinimumAge,
                items);
        }

        if (includeRecycleBin)
        {
            var recycleBin = RecycleBinInterop.Query();
            if (recycleBin.ItemCount > 0 || recycleBin.SizeBytes > 0)
            {
                items.Add(new CleanupTarget(
                    "recycle-bin",
                    CleanupTargetKind.RecycleBin,
                    $"Recycle Bin ({recycleBin.ItemCount} item(s))",
                    "Recycle Bin",
                    recycleBin.SizeBytes,
                    false));
            }
        }

        return new CleanupScanResult(items, skippedCount);
    }

    public CleanupResult Clean(IEnumerable<CleanupTarget> targets)
    {
        var deletedCount = 0;
        var skippedCount = 0;
        var freedBytes = 0L;

        foreach (var target in targets.Where(target => target.IsSelected))
        {
            if (target.Kind == CleanupTargetKind.RecycleBin)
            {
                if (RecycleBinInterop.Empty())
                {
                    deletedCount++;
                    freedBytes += target.SizeBytes;
                }
                else
                {
                    skippedCount++;
                }

                continue;
            }

            if (!IsSafeFileTarget(target))
            {
                skippedCount++;
                continue;
            }

            try
            {
                if (!File.Exists(target.Path))
                {
                    skippedCount++;
                    continue;
                }

                File.Delete(target.Path);
                deletedCount++;
                freedBytes += target.SizeBytes;
            }
            catch
            {
                skippedCount++;
            }
        }

        RemoveEmptyDirectories(userTempDirectory);
        if (windowsTempDirectory is not null)
        {
            RemoveEmptyDirectories(windowsTempDirectory);
        }

        return new CleanupResult(deletedCount, skippedCount, freedBytes);
    }

    private int ScanDirectory(
        string rootDirectory,
        CleanupTargetKind kind,
        string label,
        DateTimeOffset olderThan,
        List<CleanupTarget> items)
    {
        if (!Directory.Exists(rootDirectory))
        {
            return 0;
        }

        var skippedCount = 0;
        var pendingDirectories = new Stack<string>();
        pendingDirectories.Push(rootDirectory);

        while (pendingDirectories.Count > 0)
        {
            var directory = pendingDirectories.Pop();

            if (IsReparsePoint(directory))
            {
                skippedCount++;
                continue;
            }

            try
            {
                foreach (var subDirectory in Directory.EnumerateDirectories(directory))
                {
                    pendingDirectories.Push(subDirectory);
                }

                foreach (var filePath in Directory.EnumerateFiles(directory))
                {
                    if (TryBuildTarget(rootDirectory, filePath, kind, label, olderThan, items.Count, out var target))
                    {
                        items.Add(target);
                    }
                    else
                    {
                        skippedCount++;
                    }
                }
            }
            catch
            {
                skippedCount++;
            }
        }

        return skippedCount;
    }

    private static bool TryBuildTarget(
        string rootDirectory,
        string filePath,
        CleanupTargetKind kind,
        string label,
        DateTimeOffset olderThan,
        int index,
        out CleanupTarget target)
    {
        target = new CleanupTarget(string.Empty, kind, string.Empty, string.Empty, 0, false);

        try
        {
            if (!IsPathInsideDirectory(filePath, rootDirectory))
            {
                return false;
            }

            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists || fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                return false;
            }

            if (fileInfo.LastWriteTimeUtc > olderThan.UtcDateTime)
            {
                return false;
            }

            var relativePath = Path.GetRelativePath(rootDirectory, filePath);
            target = new CleanupTarget(
                $"{kind}-{index}",
                kind,
                $"{label}: {relativePath}",
                filePath,
                fileInfo.Length,
                true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool IsSafeFileTarget(CleanupTarget target)
    {
        return target.Kind switch
        {
            CleanupTargetKind.UserTempFile => IsPathInsideDirectory(target.Path, userTempDirectory),
            CleanupTargetKind.WindowsTempFile => windowsTempDirectory is not null && IsPathInsideDirectory(target.Path, windowsTempDirectory),
            _ => false
        };
    }

    private static bool IsPathInsideDirectory(string path, string directory)
    {
        var fullPath = Path.GetFullPath(path);
        var fullDirectory = NormalizeDirectory(directory);
        return fullPath.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeDirectory(string directory)
    {
        var fullPath = Path.GetFullPath(directory);
        return Path.EndsInDirectorySeparator(fullPath)
            ? fullPath
            : fullPath + Path.DirectorySeparatorChar;
    }

    private static bool IsReparsePoint(string path)
    {
        try
        {
            return File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint);
        }
        catch
        {
            return true;
        }
    }

    private static void RemoveEmptyDirectories(string rootDirectory)
    {
        if (!Directory.Exists(rootDirectory))
        {
            return;
        }

        IEnumerable<string> directories;
        try
        {
            directories = Directory
                .EnumerateDirectories(rootDirectory, "*", SearchOption.AllDirectories)
                .OrderByDescending(path => path.Length)
                .ToArray();
        }
        catch
        {
            return;
        }

        foreach (var directory in directories)
        {
            try
            {
                if (!IsReparsePoint(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                }
            }
            catch
            {
                // Empty-directory cleanup is best effort only.
            }
        }
    }

    private static class RecycleBinInterop
    {
        private const uint ShrbNoconfirmation = 0x00000001;
        private const uint ShrbNoprogressui = 0x00000002;
        private const uint ShrbNosound = 0x00000004;

        public static RecycleBinInfo Query()
        {
            var query = new ShQueryRbInfo
            {
                cbSize = Marshal.SizeOf<ShQueryRbInfo>()
            };

            return SHQueryRecycleBin(null, ref query) == 0
                ? new RecycleBinInfo(query.i64Size, query.i64NumItems)
                : new RecycleBinInfo(0, 0);
        }

        public static bool Empty()
        {
            return SHEmptyRecycleBin(IntPtr.Zero, null, ShrbNoconfirmation | ShrbNoprogressui | ShrbNosound) == 0;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHQueryRecycleBin(string? pszRootPath, ref ShQueryRbInfo pSHQueryRBInfo);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

        [StructLayout(LayoutKind.Sequential)]
        private struct ShQueryRbInfo
        {
            public int cbSize;
            public long i64Size;
            public long i64NumItems;
        }
    }

    private sealed record RecycleBinInfo(long SizeBytes, long ItemCount);
}
