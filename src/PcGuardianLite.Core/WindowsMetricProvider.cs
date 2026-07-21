using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace PcGuardianLite.Core;

public sealed class WindowsMetricProvider : IMetricProvider
{
    private CpuTimeSample? previousCpuSample;

    public double GetCpuUsagePercent()
    {
        try
        {
            if (!NativeMethods.GetSystemTimes(out var idleTime, out var kernelTime, out var userTime))
            {
                return 0;
            }

            var current = new CpuTimeSample(
                FileTimeToUInt64(idleTime),
                FileTimeToUInt64(kernelTime),
                FileTimeToUInt64(userTime));

            if (previousCpuSample is null)
            {
                previousCpuSample = current;
                return 0;
            }

            var previous = previousCpuSample.Value;
            previousCpuSample = current;

            var idleDelta = current.Idle - previous.Idle;
            var kernelDelta = current.Kernel - previous.Kernel;
            var userDelta = current.User - previous.User;
            var totalDelta = kernelDelta + userDelta;

            if (totalDelta == 0)
            {
                return 0;
            }

            return Math.Clamp((double)(totalDelta - idleDelta) / totalDelta * 100, 0, 100);
        }
        catch
        {
            return 0;
        }
    }

    public double GetMemoryUsagePercent()
    {
        try
        {
            var status = new NativeMethods.MemoryStatusEx();

            if (!NativeMethods.GlobalMemoryStatusEx(status) || status.TotalPhys == 0)
            {
                return 0;
            }

            var used = status.TotalPhys - status.AvailPhys;
            return Math.Clamp((double)used / status.TotalPhys * 100, 0, 100);
        }
        catch
        {
            return 0;
        }
    }

    public NetworkCounterSample GetNetworkSample()
    {
        long bytesReceived = 0;
        long bytesSent = 0;

        try
        {
            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                    networkInterface.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                var statistics = networkInterface.GetIPv4Statistics();
                bytesReceived += statistics.BytesReceived;
                bytesSent += statistics.BytesSent;
            }
        }
        catch
        {
            bytesReceived = 0;
            bytesSent = 0;
        }

        return new NetworkCounterSample(bytesReceived, bytesSent, DateTimeOffset.Now);
    }

    public double GetDiskUsagePercent()
    {
        try
        {
            var fixedDrives = DriveInfo
                .GetDrives()
                .Where(drive => drive.DriveType == DriveType.Fixed && drive.IsReady)
                .ToArray();

            var total = fixedDrives.Sum(drive => drive.TotalSize);

            if (total <= 0)
            {
                return 0;
            }

            var free = fixedDrives.Sum(drive => drive.TotalFreeSpace);
            return Math.Clamp((double)(total - free) / total * 100, 0, 100);
        }
        catch
        {
            return 0;
        }
    }

    public double? GetTemperatureCelsius()
    {
        return null;
    }

    private static ulong FileTimeToUInt64(NativeMethods.FileTime fileTime)
    {
        return ((ulong)fileTime.HighDateTime << 32) | fileTime.LowDateTime;
    }

    private readonly record struct CpuTimeSample(ulong Idle, ulong Kernel, ulong User);

    private static class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetSystemTimes(out FileTime idleTime, out FileTime kernelTime, out FileTime userTime);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatusEx lpBuffer);

        [StructLayout(LayoutKind.Sequential)]
        public struct FileTime
        {
            public uint LowDateTime;
            public uint HighDateTime;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public sealed class MemoryStatusEx
        {
            public uint Length = (uint)Marshal.SizeOf<MemoryStatusEx>();
            public uint MemoryLoad;
            public ulong TotalPhys;
            public ulong AvailPhys;
            public ulong TotalPageFile;
            public ulong AvailPageFile;
            public ulong TotalVirtual;
            public ulong AvailVirtual;
            public ulong AvailExtendedVirtual;
        }
    }
}
