using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AdaraBox.App.ViewModels;

namespace AdaraBox.App.Views.Panels;

public partial class MediaPanelView : UserControl
{
    private MediaPanelViewModel? ViewModel => DataContext as MediaPanelViewModel;

    public MediaPanelView()
    {
        InitializeComponent();
    }

    private void FormNameTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is TextBox tb && tb.IsVisible)
        {
            Dispatcher.BeginInvoke(() =>
            {
                tb.Focus();
                Keyboard.Focus(tb);
                tb.SelectAll();
            });
        }
    }

    private void FormTextBox_KeyDown(object sender, KeyEventArgs e)
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
}
