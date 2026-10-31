using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AdaraBox.App.ViewModels;

namespace AdaraBox.App.Views.Panels;

public partial class WorkspacePanelView : UserControl
{
    private WorkspacePanelViewModel? ViewModel => DataContext as WorkspacePanelViewModel;

    public WorkspacePanelView()
    {
        InitializeComponent();
    }

    private void FormNameTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is TextBox textBox && textBox.IsVisible)
        {
            Dispatcher.BeginInvoke(() =>
            {
                textBox.Focus();
                Keyboard.Focus(textBox);
                textBox.SelectAll();
            });
        }
    }

    private void FormNameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (ViewModel == null) return;

        if (e.Key == Key.Enter)
        {
            if (ViewModel.SaveFormCommand.CanExecute(null))
            {
                ViewModel.SaveFormCommand.Execute(null);
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            ViewModel.CancelFormCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void ResourceTargetTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is TextBox textBox && textBox.IsVisible)
        {
            Dispatcher.BeginInvoke(() =>
            {
                textBox.Focus();
                textBox.SelectAll();
            });
        }
    }

    private void ResourceForm_KeyDown(object sender, KeyEventArgs e)
    {
        if (ViewModel == null) return;

        if (e.Key == Key.Enter)
        {
            if (ViewModel.SaveResourceFormCommand.CanExecute(null))
            {
                ViewModel.SaveResourceFormCommand.Execute(null);
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            ViewModel.CancelResourceFormCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void WorkspaceCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // Ignore clicks on child buttons (✏, 🗑) so their own commands execute
        if (e.OriginalSource is DependencyObject source)
        {
            var btn = FindParent<Button>(source);
            if (btn != null) return;
        }

        if (sender is FrameworkElement element && element.DataContext is WorkspaceItemViewModel item)
        {
            item.OpenCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void ResourceCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // Ignore clicks directly on child buttons (like action buttons)
        if (e.OriginalSource is DependencyObject source)
        {
            var btn = FindParent<Button>(source);
            if (btn != null) return;

            // If user clicked directly on CheckBox, let WPF toggle it naturally
            var chk = FindParent<CheckBox>(source);
            if (chk != null) return;
        }

        if (ViewModel != null && ViewModel.IsOpenMode)
        {
            if (sender is FrameworkElement element && element.DataContext is ResourceItemViewModel item)
            {
                item.ToggleSelectCommand.Execute(null);
                e.Handled = true;
            }
        }
    }

    private void WorkspaceCard_KeyDown(object sender, KeyEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is WorkspaceItemViewModel item)
        {
            if (e.Key == Key.Enter)
            {
                item.OpenCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.F2)
            {
                item.RenameCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                item.DeleteCommand.Execute(null);
                e.Handled = true;
            }
        }
    }

    private void ResourceCard_KeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not ResourceItemViewModel item)
        {
            return;
        }

        if (ViewModel != null && ViewModel.IsOpenMode)
        {
            if (e.Key == Key.Space)
            {
                item.ToggleSelectCommand.Execute(null);
                e.Handled = true;
            }
        }
        else if (ViewModel != null && !ViewModel.IsOpenMode)
        {
            if (e.Key == Key.F2)
            {
                item.EditCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                item.DeleteCommand.Execute(null);
                e.Handled = true;
            }
        }
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        DependencyObject? parent = child;
        while (parent != null && parent is not T)
        {
            parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
        }
        return parent as T;
    }
}
