using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;

namespace AdaraBox.App.Services;

/// <summary>
/// Calculates window coordinates in DIPs based on current cursor monitor and per-monitor DPI.
/// Implements SRS §16 no. 2 and NFR-CP-03.
/// </summary>
public static class ScreenPositionHelper
{
    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    public static Rect GetTargetWorkAreaDip(Visual visual)
    {
        try
        {
            if (GetCursorPos(out var pt))
            {
                var hMonitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
                if (hMonitor != IntPtr.Zero)
                {
                    var mi = new MONITORINFO();
                    mi.cbSize = Marshal.SizeOf<MONITORINFO>();
                    if (GetMonitorInfo(hMonitor, ref mi))
                    {
                        var dpi = VisualTreeHelper.GetDpi(visual);
                        double scaleX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
                        double scaleY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;

                        double left = mi.rcWork.Left / scaleX;
                        double top = mi.rcWork.Top / scaleY;
                        double width = (mi.rcWork.Right - mi.rcWork.Left) / scaleX;
                        double height = (mi.rcWork.Bottom - mi.rcWork.Top) / scaleY;

                        return new Rect(left, top, width, height);
                    }
                }
            }

            return SystemParameters.WorkArea;
        }
        catch
        {
            return SystemParameters.WorkArea;
        }
    }
}
