using CommunityToolkit.Mvvm.ComponentModel;
using AdaraBox.App.Resources;

namespace AdaraBox.App.ViewModels;

public sealed class WorkspacePanelViewModel : ObservableObject
{
    public string EmptyMessage => UiText.WorkspaceEmpty;
}
