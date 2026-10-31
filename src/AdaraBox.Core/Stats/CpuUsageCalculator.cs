using System;

namespace AdaraBox.Core.Stats;

/// <summary>
/// Calculates CPU utilization from two consecutive Windows GetSystemTimes measurements.
/// Pure calculation function covered by unit tests.
/// </summary>
public static class CpuUsageCalculator
{
    public readonly record struct SystemTimesSample(ulong IdleTime, ulong KernelTime, ulong UserTime);

    /// <summary>
    /// Calculates CPU percentage from sample1 to sample2.
    /// Returns null if sample deltas are invalid or backward (counter wrap/reset).
    /// </summary>
    public static double? CalculateUsagePercent(SystemTimesSample prev, SystemTimesSample current)
    {
        if (current.IdleTime < prev.IdleTime ||
            current.KernelTime < prev.KernelTime ||
            current.UserTime < prev.UserTime)
        {
            return null;
        }

        ulong deltaIdle = current.IdleTime - prev.IdleTime;
        ulong deltaKernel = current.KernelTime - prev.KernelTime;
        ulong deltaUser = current.UserTime - prev.UserTime;

        // In Windows GetSystemTimes, KernelTime already includes IdleTime.
        ulong totalSystemTime = deltaKernel + deltaUser;
        if (totalSystemTime == 0)
        {
            return null;
        }

        if (deltaIdle > totalSystemTime)
        {
            return 0.0;
        }

        double idleFraction = (double)deltaIdle / (double)totalSystemTime;
        double usageFraction = 1.0 - idleFraction;
        double percent = usageFraction * 100.0;

        return Math.Clamp(percent, 0.0, 100.0);
    }
}
