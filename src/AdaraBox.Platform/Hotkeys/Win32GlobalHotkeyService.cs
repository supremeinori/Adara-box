using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Serilog;
using AdaraBox.Core.Hotkeys;

namespace AdaraBox.Platform.Hotkeys;

public sealed class Win32GlobalHotkeyService : IGlobalHotkeyService
{
    private const int HotkeyId = 9001;
    private const int WM_HOTKEY = 0x0312;

    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly ILogger _logger;
    private HwndSource? _hwndSource;
    private bool _isDisposed;

    public bool IsRegistered { get; private set; }
    public string? LastRegistrationError { get; private set; }
    public HotkeyDefinition? CurrentHotkey { get; private set; }

    public event EventHandler? HotkeyPressed;

    public Win32GlobalHotkeyService(ILogger? logger = null)
    {
        _logger = logger ?? Log.ForContext<Win32GlobalHotkeyService>();
        EnsureHwndSource();
    }

    private void EnsureHwndSource()
    {
        if (_hwndSource == null)
        {
            var parameters = new HwndSourceParameters("AdaraBox_HotkeyListener")
            {
                WindowStyle = 0,
                Width = 0,
                Height = 0
            };
            _hwndSource = new HwndSource(parameters);
            _hwndSource.AddHook(HwndHook);
        }
    }

    public bool Register(HotkeyDefinition hotkey)
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(Win32GlobalHotkeyService));
        if (hotkey == null) throw new ArgumentNullException(nameof(hotkey));

        Unregister();
        EnsureHwndSource();

        uint fsModifiers = MOD_NOREPEAT;
        if (hotkey.Modifiers.HasFlag(HotkeyModifier.Alt)) fsModifiers |= MOD_ALT;
        if (hotkey.Modifiers.HasFlag(HotkeyModifier.Control)) fsModifiers |= MOD_CONTROL;
        if (hotkey.Modifiers.HasFlag(HotkeyModifier.Shift)) fsModifiers |= MOD_SHIFT;
        if (hotkey.Modifiers.HasFlag(HotkeyModifier.Windows)) fsModifiers |= MOD_WIN;

        var success = RegisterHotKey(_hwndSource!.Handle, HotkeyId, fsModifiers, (uint)hotkey.KeyCode);
        if (success)
        {
            IsRegistered = true;
            LastRegistrationError = null;
            CurrentHotkey = hotkey;
            _logger.Information("Global hotkey registered successfully: {Hotkey}", hotkey.DisplayText);
            return true;
        }

        var errorCode = Marshal.GetLastWin32Error();
        IsRegistered = false;
        CurrentHotkey = null;
        LastRegistrationError = "Hotkey sudah digunakan oleh aplikasi lain atau tidak diizinkan oleh sistem.";
        _logger.Warning("Failed to register global hotkey: {Hotkey}, Win32Error={ErrorCode}", hotkey.DisplayText, errorCode);
        return false;
    }

    public void Unregister()
    {
        if (IsRegistered && _hwndSource != null)
        {
            try
            {
                UnregisterHotKey(_hwndSource.Handle, HotkeyId);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Exception unregistering hotkey");
            }
        }

        IsRegistered = false;
        CurrentHotkey = null;
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            _logger.Debug("Global hotkey pressed event received");
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
            handled = true;
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        Unregister();

        if (_hwndSource != null)
        {
            _hwndSource.RemoveHook(HwndHook);
            _hwndSource.Dispose();
            _hwndSource = null;
        }
    }
}
