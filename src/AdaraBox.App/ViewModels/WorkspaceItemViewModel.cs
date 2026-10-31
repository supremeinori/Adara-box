using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AdaraBox.Core.Workspaces;

namespace AdaraBox.App.ViewModels;

public sealed partial class WorkspaceItemViewModel : ObservableObject
{
    private readonly WorkspacePanelViewModel _parent;

    [ObservableProperty]
    private string _name;

    public Guid Id { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; }
    public int SortOrder { get; }
    public int ResourceCount => 0; // M02 scope: always 0
    public string ResourceCountText => $"{ResourceCount} resource";

    public WorkspaceItemViewModel(Workspace workspace, WorkspacePanelViewModel parent)
    {
        _parent = parent;
        Id = workspace.Id;
        _name = workspace.Name;
        CreatedAt = workspace.CreatedAt;
        UpdatedAt = workspace.UpdatedAt;
        SortOrder = workspace.SortOrder;
    }

    public void UpdateName(string newName)
    {
        Name = newName;
    }

    [RelayCommand]
    private void Open() => _parent.OpenWorkspace(this);

    [RelayCommand]
    private void Rename() => _parent.BeginRename(this);

    [RelayCommand]
    private void Delete() => _ = _parent.DeleteWorkspaceAsync(this);
}
