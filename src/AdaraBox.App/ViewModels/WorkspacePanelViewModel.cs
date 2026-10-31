using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AdaraBox.App.Resources;
using AdaraBox.App.Services;
using AdaraBox.Core.Workspaces;

namespace AdaraBox.App.ViewModels;

public sealed partial class WorkspacePanelViewModel : ObservableObject
{
    private readonly IWorkspaceService _workspaceService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isFormOpen;

    [ObservableProperty]
    private Guid? _editingWorkspaceId;

    [ObservableProperty]
    private string _formTitle = string.Empty;

    [ObservableProperty]
    private string _formName = string.Empty;

    [ObservableProperty]
    private string? _formError;

    [ObservableProperty]
    private WorkspaceItemViewModel? _selectedWorkspace;

    public ObservableCollection<WorkspaceItemViewModel> Workspaces { get; } = new();

    public bool HasWorkspaces => Workspaces.Count > 0;
    public bool IsEmpty => !IsLoading && Workspaces.Count == 0;
    public bool HasFormError => !string.IsNullOrEmpty(FormError);
    public bool IsDetailViewActive => SelectedWorkspace != null;
    public string DetailTitle => SelectedWorkspace?.Name ?? string.Empty;
    public string DetailResourceCountText => SelectedWorkspace != null ? $"{SelectedWorkspace.ResourceCount} resource" : string.Empty;

    public string EmptyTitle => UiText.WorkspaceEmpty;
    public string EmptyDescription => UiText.WorkspaceEmptyDescription;
    public string CreateFirstButtonText => UiText.CreateFirstWorkspace;
    public string DetailPlaceholderText => UiText.WorkspaceDetailResourcesPlaceholder;
    public string DetailPlaceholderSubtext => UiText.WorkspaceDetailResourcesSubtext;

    public WorkspacePanelViewModel(IWorkspaceService workspaceService, IDialogService dialogService)
    {
        _workspaceService = workspaceService;
        _dialogService = dialogService;

        _ = LoadWorkspacesAsync();
    }

    [RelayCommand]
    public async Task LoadWorkspacesAsync()
    {
        try
        {
            IsLoading = true;
            NotifyListStateChanged();

            Workspaces.Clear();
            var list = await _workspaceService.ListAsync();
            foreach (var ws in list)
            {
                Workspaces.Add(new WorkspaceItemViewModel(ws, this));
            }
        }
        finally
        {
            IsLoading = false;
            NotifyListStateChanged();
        }
    }

    [RelayCommand]
    public void BeginCreate()
    {
        EditingWorkspaceId = null;
        FormTitle = UiText.CreateWorkspaceTitle;
        FormName = string.Empty;
        FormError = null;
        IsFormOpen = true;
    }

    [RelayCommand]
    public void BeginRename(WorkspaceItemViewModel item)
    {
        EditingWorkspaceId = item.Id;
        FormTitle = UiText.RenameWorkspaceTitle;
        FormName = item.Name;
        FormError = null;
        IsFormOpen = true;
    }

    [RelayCommand]
    public async Task SaveFormAsync()
    {
        FormError = null;

        if (EditingWorkspaceId == null)
        {
            var result = await _workspaceService.CreateAsync(FormName);
            if (!result.Success)
            {
                FormError = MapError(result.Error?.Code);
                return;
            }

            var item = new WorkspaceItemViewModel(result.Value!, this);
            Workspaces.Add(item);
            IsFormOpen = false;
            FormName = string.Empty;
            NotifyListStateChanged();
            OpenWorkspace(item);
        }
        else
        {
            var id = EditingWorkspaceId.Value;
            var result = await _workspaceService.RenameAsync(id, FormName);
            if (!result.Success)
            {
                FormError = MapError(result.Error?.Code);
                return;
            }

            var item = Workspaces.FirstOrDefault(w => w.Id == id);
            item?.UpdateName(FormName.Trim());

            if (SelectedWorkspace?.Id == id)
            {
                OnPropertyChanged(nameof(DetailTitle));
            }

            IsFormOpen = false;
            EditingWorkspaceId = null;
            FormName = string.Empty;
            NotifyListStateChanged();
        }
    }

    [RelayCommand]
    public void CancelForm()
    {
        IsFormOpen = false;
        EditingWorkspaceId = null;
        FormName = string.Empty;
        FormError = null;
    }

    public void OpenWorkspace(WorkspaceItemViewModel item)
    {
        SelectedWorkspace = item;
        OnPropertyChanged(nameof(IsDetailViewActive));
        OnPropertyChanged(nameof(DetailTitle));
        OnPropertyChanged(nameof(DetailResourceCountText));
    }

    [RelayCommand]
    public void BackToList()
    {
        SelectedWorkspace = null;
        OnPropertyChanged(nameof(IsDetailViewActive));
        OnPropertyChanged(nameof(DetailTitle));
        OnPropertyChanged(nameof(DetailResourceCountText));
    }

    [RelayCommand]
    public async Task DeleteWorkspaceAsync(WorkspaceItemViewModel? item)
    {
        if (item == null) return;

        var confirmed = await _dialogService.ConfirmDeleteWorkspaceAsync(item.Name, item.ResourceCount);
        if (!confirmed) return;

        var result = await _workspaceService.DeleteAsync(item.Id);
        if (!result.Success)
        {
            await _dialogService.ShowErrorAsync(UiText.StatusError, MapError(result.Error?.Code));
            return;
        }

        Workspaces.Remove(item);
        if (SelectedWorkspace?.Id == item.Id)
        {
            SelectedWorkspace = null;
            OnPropertyChanged(nameof(IsDetailViewActive));
        }

        NotifyListStateChanged();
    }

    private void NotifyListStateChanged()
    {
        OnPropertyChanged(nameof(HasWorkspaces));
        OnPropertyChanged(nameof(IsEmpty));
    }

    partial void OnFormErrorChanged(string? value)
    {
        OnPropertyChanged(nameof(HasFormError));
    }

    private static string MapError(string? errorCode) => errorCode switch
    {
        "workspace.name.empty" => UiText.ErrorWorkspaceNameEmpty,
        "workspace.name.too_long" => UiText.ErrorWorkspaceNameTooLong,
        "workspace.name.invalid" => UiText.ErrorWorkspaceNameInvalid,
        "workspace.not_found" => UiText.ErrorWorkspaceNotFound,
        _ => UiText.ErrorUnknown
    };
}
