using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AdaraBox.App.Resources;
using AdaraBox.Core.Common;

namespace AdaraBox.App.Views.Components;

public partial class ModuleStatusView : UserControl
{
    public static readonly DependencyProperty StatusProperty =
        DependencyProperty.Register(
            nameof(Status),
            typeof(ModuleStatus),
            typeof(ModuleStatusView),
            new PropertyMetadata(ModuleStatus.InDevelopment, OnPropertyChanged));

    public static readonly DependencyProperty StatusMessageProperty =
        DependencyProperty.Register(
            nameof(StatusMessage),
            typeof(string),
            typeof(ModuleStatusView),
            new PropertyMetadata(null, OnPropertyChanged));

    public ModuleStatus Status
    {
        get => (ModuleStatus)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public string? StatusMessage
    {
        get => (string?)GetValue(StatusMessageProperty);
        set => SetValue(StatusMessageProperty, value);
    }

    public ModuleStatusView()
    {
        InitializeComponent();
        UpdateVisuals();
    }

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ModuleStatusView view)
        {
            view.UpdateVisuals();
        }
    }

    public void UpdateVisuals()
    {
        var (badgeText, icon, bgBrush, borderBrush, textBrush) = GetVisualProps(Status, StatusMessage);

        StatusBadgeText.Text = badgeText;
        StatusBadgeIcon.Text = icon;
        BadgeBorder.Background = bgBrush;
        BadgeBorder.BorderBrush = borderBrush;
        StatusBadgeText.Foreground = textBrush;

        if (!string.IsNullOrWhiteSpace(StatusMessage) && StatusMessage != badgeText)
        {
            DetailMessageText.Text = StatusMessage;
            DetailMessageText.Visibility = Visibility.Visible;
        }
        else
        {
            DetailMessageText.Visibility = Visibility.Collapsed;
        }
    }

    public static (string Text, string Icon, Brush Bg, Brush Border, Brush TextBrush) GetVisualProps(ModuleStatus status, string? customMessage)
    {
        var text = status.ToDisplayText(customMessage);

        return status switch
        {
            ModuleStatus.InDevelopment => (
                text,
                "🛠",
                new SolidColorBrush(Color.FromRgb(0x3B, 0x2E, 0x05)),
                new SolidColorBrush(Color.FromRgb(0xD9, 0x77, 0x06)),
                new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24))
            ),
            ModuleStatus.NotAvailable => (
                text,
                "⏸",
                new SolidColorBrush(Color.FromRgb(0x27, 0x27, 0x2A)),
                new SolidColorBrush(Color.FromRgb(0x71, 0x71, 0x7A)),
                new SolidColorBrush(Color.FromRgb(0xD4, 0xD4, 0xD8))
            ),
            ModuleStatus.Unsupported => (
                text,
                "🚫",
                new SolidColorBrush(Color.FromRgb(0x43, 0x14, 0x07)),
                new SolidColorBrush(Color.FromRgb(0xEA, 0x58, 0x0C)),
                new SolidColorBrush(Color.FromRgb(0xFB, 0x92, 0x3C))
            ),
            ModuleStatus.Error => (
                text,
                "⚠️",
                new SolidColorBrush(Color.FromRgb(0x45, 0x0A, 0x0A)),
                new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26)),
                new SolidColorBrush(Color.FromRgb(0xF8, 0x71, 0x71))
            ),
            _ => (
                text,
                "✔",
                new SolidColorBrush(Color.FromRgb(0x06, 0x4E, 0x3B)),
                new SolidColorBrush(Color.FromRgb(0x05, 0x96, 0x69)),
                new SolidColorBrush(Color.FromRgb(0x34, 0xD3, 0x99))
            )
        };
    }
}
