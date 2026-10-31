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
    public const double TopBarCompactWidthDip = 480.0;
    public const double TopBarDefaultWidthDip = 480.0;

    /// <summary>
    /// Default expanded width for module panels in DIP.
    /// </summary>
    public const double PanelDefaultWidthDip = 560.0;

    /// <summary>
    /// Maximum panel width ratio relative to the monitor work area width.
    /// </summary>
    public const double PanelMaxScreenWidthRatio = 0.90;

    /// <summary>
    /// Maximum panel height ratio relative to the monitor work area height.
    /// </summary>
    public const double PanelMaxScreenHeightRatio = 0.70;

    /// <summary>
    /// Panel horizontal expansion animation duration in milliseconds.
    /// </summary>
    public const int PanelAnimationDurationMs = 180;

    /// <summary>
    /// Policy determining whether module panels close automatically when window loses focus.
    /// Default is false (panels remain open until explicitly closed by user or Esc).
    /// </summary>
    public const bool ClosePanelOnFocusLost = false;

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

    /// <summary>
    /// Default hardware stats polling interval in milliseconds (FR-ST-05, SRS §9.4).
    /// </summary>
    public const int StatsPollingIntervalMs = 1000;
}
