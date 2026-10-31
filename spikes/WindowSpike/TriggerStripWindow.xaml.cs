using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using AdaraBox.Core.Common;

namespace AdaraBox.WindowSpike;

public partial class TriggerStripWindow : Window
{
    private readonly TopBarWindow _topBar;
    private readonly DispatcherTimer _dwellTimer;
    private readonly DispatcherTimer _hideTimer;
    private readonly Action<string> _logger;

    public TriggerStripWindow(TopBarWindow topBar, Action<string> logger)
    {
        _topBar = topBar;
        _logger = logger;
        InitializeComponent();

        Height = UiConstants.HoverStripHeightDip;
        Width = UiConstants.TopBarDefaultWidthDip;

        _dwellTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(UiConstants.HoverDwellTimeMs) };
        _dwellTimer.Tick += DwellTimer_Tick;

        _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(UiConstants.HoverHideDelayMs) };
        _hideTimer.Tick += HideTimer_Tick;

        Loaded += TriggerStripWindow_Loaded;
        MouseEnter += TriggerStripWindow_MouseEnter;
        MouseLeave += TriggerStripWindow_MouseLeave;

        _topBar.MouseEnter += (s, e) =>
        {
            _hideTimer.Stop();
            _logger("Cursor entered Top Bar: hide timer cancelled.");
        };

        _topBar.MouseLeave += (s, e) =>
        {
            _hideTimer.Start();
            _logger($"Cursor left Top Bar: hide timer started ({UiConstants.HoverHideDelayMs}ms).");
        };
    }

    private void TriggerStripWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        Left = (screenWidth - Width) / 2.0;
        Top = 0;
    }

    private void TriggerStripWindow_MouseEnter(object sender, MouseEventArgs e)
    {
        _hideTimer.Stop();
        _dwellTimer.Start();
        _logger($"Cursor entered trigger strip: dwell timer started ({UiConstants.HoverDwellTimeMs}ms).");
    }

    private void TriggerStripWindow_MouseLeave(object sender, MouseEventArgs e)
    {
        _dwellTimer.Stop();
        _logger("Cursor left trigger strip before dwell: cancelled show.");
        if (_topBar.IsVisible)
        {
            _hideTimer.Start();
        }
    }

    private void DwellTimer_Tick(object? sender, EventArgs e)
    {
        _dwellTimer.Stop();
        _topBar.Show();
        _logger("Dwell threshold reached: Top Bar shown.");
    }

    private void HideTimer_Tick(object? sender, EventArgs e)
    {
        _hideTimer.Stop();
        _topBar.Hide();
        _logger("Hide delay elapsed: Top Bar hidden.");
    }
}
