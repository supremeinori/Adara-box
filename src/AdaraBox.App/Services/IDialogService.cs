using System.Threading.Tasks;

namespace AdaraBox.App.Services;

/// <summary>
/// Abstraction for user dialogs, allowing ViewModels to be unit tested without WPF message boxes.
/// </summary>
public interface IDialogService
{
    Task<bool> ConfirmDeleteWorkspaceAsync(string workspaceName, int resourceCount);
    Task<bool> ConfirmDeleteResourceAsync(string resourceName);
    Task ShowErrorAsync(string title, string message);
}
