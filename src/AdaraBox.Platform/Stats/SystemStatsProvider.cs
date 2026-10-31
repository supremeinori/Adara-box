using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AdaraBox.Core.Stats;
using Serilog;

namespace AdaraBox.Platform.Stats;

/// <summary>
/// Real-time hardware metric provider using Win32 API and Performance Counters.
/// Never throws unhandled exceptions (FR-ST-06, SRS §9.4).
/// </summary>
public sealed class SystemStatsProvider : ISystemStatsProvider
{
    private readonly ILogger _logger = Log.ForContext<SystemStatsProvider>();

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;

        public ulong ToUlong() => ((ulong)dwHighDateTime << 32) | dwLowDateTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    private CpuUsageCalculator.SystemTimesSample? _lastCpuSample;
    private readonly object _cpuLock = new();

    private bool _gpuCategoryChecked;
    private bool _gpuCategoryAvailable;
    private List<PerformanceCounter>? _gpuCounters;

    public Task<StatsSnapshot> ReadAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        double? cpu = ReadCpuPercent();
        (long? memUsed, long? memTotal) = ReadMemory();
        IReadOnlyList<DriveUsage> drives = ReadDrives();
        double? gpu = ReadGpuPercent();

        var snapshot = new StatsSnapshot(
            DateTimeOffset.UtcNow,
            cpu,
            memUsed,
            memTotal,
            drives,
            gpu);

        return Task.FromResult(snapshot);
    }

    private double? ReadCpuPercent()
    {
        try
        {
            if (!GetSystemTimes(out var idle, out var kernel, out var user))
            {
                return null;
            }

            var current = new CpuUsageCalculator.SystemTimesSample(
                idle.ToUlong(),
                kernel.ToUlong(),
                user.ToUlong());

            lock (_cpuLock)
            {
                if (!_lastCpuSample.HasValue)
                {
                    _lastCpuSample = current;
                    return null;
                }

                var prev = _lastCpuSample.Value;
                _lastCpuSample = current;

                return CpuUsageCalculator.CalculateUsagePercent(prev, current);
            }
        }
        catch (Exception ex)
        {
            _logger.Debug(ex, "Failed to read CPU system times");
            return null;
        }
    }

    private (long? Used, long? Total) ReadMemory()
    {
        try
        {
            var memStatus = new MEMORYSTATUSEX();
            memStatus.dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>();

            if (GlobalMemoryStatusEx(ref memStatus))
            {
                long total = (long)memStatus.ullTotalPhys;
                long avail = (long)memStatus.ullAvailPhys;
                long used = total > avail ? total - avail : 0;
                return (used, total);
            }

            return (null, null);
        }
        catch (Exception ex)
        {
            _logger.Debug(ex, "Failed to read memory status");
            return (null, null);
        }
    }

    private IReadOnlyList<DriveUsage> ReadDrives()
    {
        var list = new List<DriveUsage>();
        try
        {
            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives)
            {
                try
                {
                    if (drive.DriveType == DriveType.Fixed && drive.IsReady)
                    {
                        list.Add(new DriveUsage(
                            drive.Name,
                            drive.TotalSize,
                            drive.AvailableFreeSpace));
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug(ex, "Error reading drive info for {DriveName}", drive.Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Debug(ex, "Error enumerating system drives");
        }

        return list;
    }

    private double? ReadGpuPercent()
    {
        try
        {
            if (!_gpuCategoryChecked)
            {
                _gpuCategoryChecked = true;
                _gpuCategoryAvailable = PerformanceCounterCategory.Exists("GPU Engine");
                if (_gpuCategoryAvailable)
                {
                    InitializeGpuCounters();
                }
            }

            if (!_gpuCategoryAvailable || _gpuCounters == null || _gpuCounters.Count == 0)
            {
                return null;
            }

            double totalGpu = 0.0;
            foreach (var counter in _gpuCounters)
            {
                try
                {
                    totalGpu += counter.NextValue();
                }
                catch
                {
                    // If an instance terminates, ignore this sample
                }
            }

            return Math.Clamp(totalGpu, 0.0, 100.0);
        }
        catch (Exception ex)
        {
            _logger.Debug(ex, "GPU performance counters unavailable or threw exception");
            return null;
        }
    }

    private void InitializeGpuCounters()
    {
        try
        {
            var category = new PerformanceCounterCategory("GPU Engine");
            var instanceNames = category.GetInstanceNames();

            // Filter for 3D engine instances (engtype_3D)
            var eng3DInstances = instanceNames
                .Where(name => name.Contains("engtype_3D", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (eng3DInstances.Count == 0)
            {
                _gpuCategoryAvailable = false;
                return;
            }

            _gpuCounters = new List<PerformanceCounter>();
            foreach (var instance in eng3DInstances)
            {
                try
                {
                    var pc = new PerformanceCounter("GPU Engine", "Utilization Percentage", instance, readOnly: true);
                    // First NextValue() establishes baseline
                    _ = pc.NextValue();
                    _gpuCounters.Add(pc);
                }
                catch
                {
                    // Skip inaccessible counters
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Debug(ex, "Failed to initialize GPU performance counters");
            _gpuCategoryAvailable = false;
        }
    }
}
