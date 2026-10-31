namespace AdaraBox.Core.Stats;

/// <summary>
/// Represents disk space utilization for a fixed storage volume.
/// </summary>
public sealed record DriveUsage(string Name, long TotalBytes, long FreeBytes)
{
    public long UsedBytes => TotalBytes > FreeBytes ? TotalBytes - FreeBytes : 0;
    public double UsedPercent => TotalBytes > 0 ? (double)UsedBytes / TotalBytes * 100.0 : 0.0;
}
