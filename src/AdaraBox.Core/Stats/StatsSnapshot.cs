using System;
using System.Collections.Generic;

namespace AdaraBox.Core.Stats;

/// <summary>
/// Point-in-time snapshot of system hardware metrics.
/// Null properties represent metrics that are unavailable or unsupported on the current machine (FR-ST-06).
/// </summary>
public sealed record StatsSnapshot(
    DateTimeOffset Timestamp,
    double? CpuPercent,
    long? MemoryUsedBytes,
    long? MemoryTotalBytes,
    IReadOnlyList<DriveUsage> Drives,
    double? GpuPercent)
{
    public double? MemoryPercent =>
        MemoryUsedBytes.HasValue && MemoryTotalBytes.HasValue && MemoryTotalBytes.Value > 0
            ? (double)MemoryUsedBytes.Value / MemoryTotalBytes.Value * 100.0
            : null;
}
