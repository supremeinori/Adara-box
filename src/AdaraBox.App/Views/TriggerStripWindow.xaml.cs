using System.Windows;
using System.Windows.Input;
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
    }

    private void TriggerStripWindow_Loaded(object sender, RoutedEventArgs e)
    {
        PositionAtTopCenter();
    }

    public void PositionAtTopCenter()
    {
        var workArea = SystemParameters.WorkArea;
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
