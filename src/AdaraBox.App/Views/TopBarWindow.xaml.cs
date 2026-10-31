using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using AdaraBox.App.ViewModels;
using AdaraBox.Core.Common;
using AdaraBox.Core.Modules;
using AdaraBox.Core.Visibility;

namespace AdaraBox.App.Views;

public partial class TopBarWindow : Window
{
    private TopBarViewModel? ViewModel => DataContext as TopBarViewModel;

    public TopBarWindow()
    {
        InitializeComponent();

        Loaded += TopBarWindow_Loaded;
        KeyDown += TopBarWindow_KeyDown;
        MouseEnter += TopBarWindow_MouseEnter;
        MouseLeave += TopBarWindow_MouseLeave;
        DataContextChanged += TopBarWindow_DataContextChanged;
    }

    private void TopBarWindow_Loaded(object sender, RoutedEventArgs e)
    {
        PositionAtTopCenter(Width);
    }

    private void TopBarWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is TopBarViewModel oldVm)
        {
            oldVm.PropertyChanged -= ViewModel_PropertyChanged;
        }
        if (e.NewValue is TopBarViewModel newVm)
        {
            newVm.PropertyChanged += ViewModel_PropertyChanged;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TopBarViewModel.SelectedModule) ||
            e.PropertyName == nameof(TopBarViewModel.IsPanelOpen))
        {
            UpdatePanelState(animate: true);
        }
    }

    private void PositionAtTopCenter(double targetWidth)
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Left + ((workArea.Width - targetWidth) / 2.0);
        Top = workArea.Top;
    }

    private void UpdatePanelState(bool animate)
    {
        if (ViewModel == null) return;

        bool isOpen = ViewModel.IsPanelOpen;
        double targetWidth = isOpen ? UiConstants.PanelDefaultWidthDip : UiConstants.TopBarCompactWidthDip;

        // Ensure target width does not exceed 90% screen width
        var maxAllowedWidth = SystemParameters.WorkArea.Width * UiConstants.PanelMaxScreenWidthRatio;
        if (targetWidth > maxAllowedWidth)
        {
            targetWidth = maxAllowedWidth;
        }

        PanelContainer.Visibility = isOpen ? Visibility.Visible : Visibility.Collapsed;
        ClosePanelButton.Visibility = isOpen ? Visibility.Visible : Visibility.Collapsed;

        HighlightSelectedButton();

        if (animate && IsLoaded)
        {
            AnimateWidthAndCenter(targetWidth);
        }
        else
        {
            Width = targetWidth;
            PositionAtTopCenter(targetWidth);
        }
    }

    private void AnimateWidthAndCenter(double targetWidth)
    {
        var duration = TimeSpan.FromMilliseconds(UiConstants.PanelAnimationDurationMs);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        var startWidth = Width;
        var startLeft = Left;
        var workArea = SystemParameters.WorkArea;
        var targetLeft = workArea.Left + ((workArea.Width - targetWidth) / 2.0);

        var widthAnim = new DoubleAnimation(startWidth, targetWidth, duration) { EasingFunction = ease };
        var leftAnim = new DoubleAnimation(startLeft, targetLeft, duration) { EasingFunction = ease };

        BeginAnimation(WidthProperty, widthAnim);
        BeginAnimation(LeftProperty, leftAnim);
    }

    private void HighlightSelectedButton()
    {
        if (ViewModel == null) return;
        var selectedId = ViewModel.SelectedModule?.Id;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(ModulesItemsControl); i++)
        {
            // Iterate and set styles
        }

        // Apply visual active state to items
        foreach (var item in ModulesItemsControl.Items)
        {
            if (ModulesItemsControl.ItemContainerGenerator.ContainerFromItem(item) is FrameworkElement container)
            {
                var btn = FindVisualChild<Button>(container);
                if (btn != null && item is IAppModule module)
                {
                    bool isSelected = module.Id == selectedId;
                    btn.Background = isSelected ? new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB)) : Brushes.Transparent;
                    btn.Foreground = isSelected ? Brushes.White : new SolidColorBrush(Color.FromRgb(0xD4, 0xD4, 0xD8));
                    btn.BorderBrush = isSelected ? new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6)) : Brushes.Transparent;
                }
            }
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild) return typedChild;
            var descendant = FindVisualChild<T>(child);
            if (descendant != null) return descendant;
        }
        return null;
    }

    private void ModuleButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is IAppModule module && ViewModel != null)
        {
            ViewModel.SelectModuleCommand.Execute(module);
        }
    }

    private void ClosePanelButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.ClosePanelCommand.Execute(null);
    }

    private void TopBarWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (ViewModel != null && ViewModel.IsPanelOpen)
            {
                if (ViewModel.ActivePanelViewModel is WorkspacePanelViewModel wsVm)
                {
                    if (wsVm.IsFormOpen)
                    {
                        wsVm.CancelFormCommand.Execute(null);
                        e.Handled = true;
                        return;
                    }

                    if (wsVm.IsResourceFormOpen)
                    {
                        wsVm.CancelResourceFormCommand.Execute(null);
                        e.Handled = true;
                        return;
                    }
                }

                ViewModel.ClosePanelCommand.Execute(null);
                e.Handled = true;
            }
        }
    }

    private void TopBarWindow_MouseEnter(object sender, MouseEventArgs e)
    {
        ViewModel?.VisibilityService.NotifyPointerEnteredTopBar();
    }

    private void TopBarWindow_MouseLeave(object sender, MouseEventArgs e)
    {
        ViewModel?.VisibilityService.NotifyPointerLeftTopBar();
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        if (UiConstants.ClosePanelOnFocusLost && ViewModel != null && ViewModel.IsPanelOpen)
        {
            ViewModel.ClosePanelCommand.Execute(null);
        }
    }
}
