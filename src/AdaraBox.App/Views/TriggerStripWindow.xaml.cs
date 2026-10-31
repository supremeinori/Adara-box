using System;
using System.Windows;
using System.Windows.Input;
using AdaraBox.App.Services;
using AdaraBox.Core.Common;
using AdaraBox.Core.Visibility;

namespace AdaraBox.App.Views;

public partial class TriggerStripWindow : Window
{
    private readonly ITopBarVisibilityService _visibilityService;

    public TriggerStripWindow(ITopBarVisibilityService visibilityService)
    {
        _visibilityService = visibilityService;
        InitializeComponent();

        Height = UiConstants.HoverStripHeightDip;
        Width = UiConstants.TopBarCompactWidthDip;

        Loaded += TriggerStripWindow_Loaded;
        MouseEnter += TriggerStripWindow_MouseEnter;
        MouseLeave += TriggerStripWindow_MouseLeave;

        Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        Closed += (s, e) => Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(PositionAtTopCenter);
    }

    private void TriggerStripWindow_Loaded(object sender, RoutedEventArgs e)
    {
        PositionAtTopCenter();
    }

    public void PositionAtTopCenter()
    {
        var workArea = ScreenPositionHelper.GetTargetWorkAreaDip(this);
        Left = workArea.Left + ((workArea.Width - Width) / 2.0);
        Top = workArea.Top;
    }

    private void TriggerStripWindow_MouseEnter(object sender, MouseEventArgs e)
    {
        _visibilityService.NotifyPointerEnteredTrigger();
    }

    private void TriggerStripWindow_MouseLeave(object sender, MouseEventArgs e)
    {
        _visibilityService.NotifyPointerLeftTrigger();
    }
}
