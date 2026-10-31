using System;
using System.Windows;

namespace AdaraBox.WindowSpike;

public partial class MainWindow : Window
{
    private TopBarWindow? _topBarWindow;
    private TriggerStripWindow? _triggerStripWindow;

    public MainWindow()
    {
        InitializeComponent();
        Log("Window Spike control panel started.");
        Log($"Current PrimaryScreen: {SystemParameters.PrimaryScreenWidth}x{SystemParameters.PrimaryScreenHeight}");
    }

    private void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        LogTextBox.AppendText($"[{timestamp}] {message}\n");
        LogTextBox.ScrollToEnd();
    }

    private void BtnOverlay_Click(object sender, RoutedEventArgs e)
    {
        CloseActiveSpikes();
        Log("Launching Variant 1: Overlay Topmost (ShowActivated=False)...");
        _topBarWindow = new TopBarWindow(useNoActivate: false, asAppBar: false);
        _topBarWindow.Show();
        Log("Overlay Topmost window shown. Verify that active window focus was preserved and TextBox is editable when clicked.");
        FooterStatus.Text = "Variant 1 running (Overlay Topmost).";
    }

    private void BtnNoActivate_Click(object sender, RoutedEventArgs e)
    {
        CloseActiveSpikes();
        Log("Launching Variant 1 with WS_EX_NOACTIVATE...");
        _topBarWindow = new TopBarWindow(useNoActivate: true, asAppBar: false);
        _topBarWindow.Show();
        Log("WS_EX_NOACTIVATE window shown. Try clicking the TextBox inside the top bar and typing. Notice that text input is disabled.");
        FooterStatus.Text = "Testing WS_EX_NOACTIVATE (Input disabled limitation).";
    }

    private void BtnAppBar_Click(object sender, RoutedEventArgs e)
    {
        CloseActiveSpikes();
        Log("Launching Variant 2: AppBar via SHAppBarMessage...");
        _topBarWindow = new TopBarWindow(useNoActivate: false, asAppBar: true);
        _topBarWindow.Show();
        Log("AppBar registered. Primary screen work area is now reserved; other maximized windows are shifted down.");
        FooterStatus.Text = "Variant 2 running (AppBar registered).";
    }

    private void BtnHover_Click(object sender, RoutedEventArgs e)
    {
        CloseActiveSpikes();
        Log("Launching Spike B: Hover Trigger Strip + Hidden Top Bar...");
        _topBarWindow = new TopBarWindow(useNoActivate: false, asAppBar: false);
        // Initially hidden
        _topBarWindow.Visibility = Visibility.Collapsed;

        _triggerStripWindow = new TriggerStripWindow(_topBarWindow, msg => Dispatcher.Invoke(() => Log(msg)));
        _triggerStripWindow.Show();

        Log("Hover trigger strip active at top screen edge (Height=3px, HitTestable alpha=#01000000).");
        Log("Move cursor to top center and hover >= 250ms to reveal bar.");
        FooterStatus.Text = "Spike B running (Hover trigger active).";
    }

    private void BtnCloseAll_Click(object sender, RoutedEventArgs e)
    {
        CloseActiveSpikes();
        Log("All active spike windows closed.");
        FooterStatus.Text = "All spikes closed.";
    }

    private void CloseActiveSpikes()
    {
        if (_triggerStripWindow != null)
        {
            _triggerStripWindow.Close();
            _triggerStripWindow = null;
        }
        if (_topBarWindow != null)
        {
            _topBarWindow.Close();
            _topBarWindow = null;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        CloseActiveSpikes();
        base.OnClosed(e);
    }
}
