using System.Threading.Tasks;
using System.Windows;
using AdaraBox.App.Resources;

namespace AdaraBox.App.Services;

/// <summary>
/// WPF implementation of IDialogService using standard MessageBox.
/// </summary>
public sealed class WpfDialogService : IDialogService
{
    public Task<bool> ConfirmDeleteWorkspaceAsync(string workspaceName, int resourceCount)
    {
        var message = string.Format(UiText.DeleteWorkspaceConfirm, workspaceName, resourceCount);
        var result = MessageBox.Show(
            message,
            UiText.DeleteWorkspaceTitle,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        return Task.FromResult(result == MessageBoxResult.Yes);
    }

    public Task ShowErrorAsync(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        return Task.CompletedTask;
    }
}
