namespace AdaraBox.Core.Common;

/// <summary>
/// Constants for Top Bar UI dimensions, hover triggers, and timing delays.
/// Identified and confirmed in Spike A and Spike B (Module M00).
/// </summary>
public static class UiConstants
{
    /// <summary>
    /// Default height for the compact Top Bar in device-independent pixels (DIP).
    /// </summary>
    public const double TopBarHeightDip = 36.0;

    /// <summary>
    /// Default width for the compact Top Bar in device-independent pixels (DIP).
    /// </summary>
    public const double TopBarDefaultWidthDip = 600.0;

    /// <summary>
    /// Height of the hover trigger strip at the very top edge of the screen (DIP).
    /// Kept thin (2-4 DIP) to avoid accidental triggers when interacting with window titlebars.
    /// </summary>
    public const double HoverStripHeightDip = 3.0;

    /// <summary>
    /// Minimum mouse dwell time (in milliseconds) before the hidden Top Bar reveals itself.
    /// Prevents unintentional activation during rapid mouse gestures across the top screen edge.
    /// </summary>
    public const int HoverDwellTimeMs = 250;

    /// <summary>
    /// Grace period delay (in milliseconds) before hiding the Top Bar after mouse cursor leaves.
    /// Prevents jitter if the cursor briefly crosses out of the window boundary.
    /// </summary>
    public const int HoverHideDelayMs = 600;

    /// <summary>
    /// Almost transparent color (#01000000: alpha=1/255) allowing mouse hit-testing in WPF.
    /// 100% transparent (#00000000) windows fail WPF hit-testing.
    /// </summary>
    public const string HitTestableTransparentColorHex = "#01000000";
}
