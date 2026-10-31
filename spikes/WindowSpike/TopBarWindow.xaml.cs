using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using AdaraBox.Core.Common;

namespace AdaraBox.WindowSpike;

public partial class TopBarWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    // AppBar Win32 API
    private const int ABM_NEW = 0x00000000;
    private const int ABM_REMOVE = 0x00000001;
    private const int ABM_QUERYPOS = 0x00000002;
    private const int ABM_SETPOS = 0x00000003;
    private const int ABE_TOP = 1;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct APPBARDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uCallbackMessage;
        public int uEdge;
        public RECT rc;
        public IntPtr lParam;
    }

    [DllImport("shell32.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern uint SHAppBarMessage(int dwMessage, ref APPBARDATA pData);

    private bool _isAppBarRegistered = false;
    private readonly bool _useNoActivate;
    private readonly bool _asAppBar;

    public TopBarWindow(bool useNoActivate = false, bool asAppBar = false)
    {
        _useNoActivate = useNoActivate;
        _asAppBar = asAppBar;
        InitializeComponent();

        Loaded += TopBarWindow_Loaded;
        Closing += TopBarWindow_Closing;
    }

    private void TopBarWindow_Loaded(object sender, RoutedEventArgs e)
    {
        PositionAtTopCenter();

        var hwnd = new WindowInteropHelper(this).Handle;

        if (_useNoActivate)
        {
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE);
            StatusText.Text = "NoActivate (Input Disabled)";
            StatusText.Foreground = System.Windows.Media.Brushes.Orange;
        }

        if (_asAppBar)
        {
            RegisterAsAppBar(hwnd);
            StatusText.Text = "AppBar Mode (Space Reserved)";
            StatusText.Foreground = System.Windows.Media.Brushes.Cyan;
        }
    }

    private void PositionAtTopCenter()
    {
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        Left = (screenWidth - Width) / 2.0;
        Top = 0;
    }

    private void RegisterAsAppBar(IntPtr hwnd)
    {
        var abd = new APPBARDATA
        {
            cbSize = Marshal.SizeOf<APPBARDATA>(),
            hWnd = hwnd,
            uEdge = ABE_TOP,
            rc = new RECT
            {
                left = (int)Left,
                top = 0,
                right = (int)(Left + Width),
                bottom = (int)Height
            }
        };

        SHAppBarMessage(ABM_NEW, ref abd);
        SHAppBarMessage(ABM_QUERYPOS, ref abd);
        SHAppBarMessage(ABM_SETPOS, ref abd);
        _isAppBarRegistered = true;
    }

    private void UnregisterAppBar()
    {
        if (_isAppBarRegistered)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            var abd = new APPBARDATA
            {
                cbSize = Marshal.SizeOf<APPBARDATA>(),
                hWnd = hwnd
            };
            SHAppBarMessage(ABM_REMOVE, ref abd);
            _isAppBarRegistered = false;
        }
    }

    private void TopBarWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        UnregisterAppBar();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
