using System;
using System.Globalization;

namespace AdaraBox.Core.Stats;

/// <summary>
/// Formats byte quantities into localized human-readable size strings (KB, MB, GB, TB).
/// </summary>
public static class SizeFormatter
{
    private static readonly string[] Units = { "B", "KB", "MB", "GB", "TB", "PB" };

    public static string FormatBytes(long bytes, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;

        if (bytes <= 0) return $"0 {Units[0]}";

        int unitIndex = 0;
        double size = bytes;

        while (size >= 1024.0 && unitIndex < Units.Length - 1)
        {
            size /= 1024.0;
            unitIndex++;
        }

        // Show 1 decimal place for MB and above, 0 for B and KB
        string format = unitIndex >= 2 ? "F1" : "F0";
        return $"{size.ToString(format, culture)} {Units[unitIndex]}";
    }

    public static string FormatPercent(double? percent, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        if (!percent.HasValue) return "Tidak tersedia";
        return $"{percent.Value.ToString("F0", culture)}%";
    }
}
